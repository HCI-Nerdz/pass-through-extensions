<#
.SYNOPSIS
  Install HCI Nerdz pass-through extensions for the current user.

.DESCRIPTION
  Copies the open broker into %LOCALAPPDATA%\HCI-Nerdz\pass-through-extensions and
  registers allow-listed meta-suffixes under HKCU\Software\Classes so Explorer
  double-click peels to the inner file type.

.PARAMETER WhatIf
  Show actions without writing registry or files.
#>
[CmdletBinding(SupportsShouldProcess = $true)]
param()

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'PassThrough.Common.ps1')

$installRoot = Get-InstallRoot
$openScript = Join-Path $installRoot 'Open-PassThrough.ps1'
$commonScript = Join-Path $installRoot 'PassThrough.Common.ps1'
$pwsh = Join-Path $PSHOME 'pwsh.exe'
if (-not (Test-Path $pwsh)) {
    $pwsh = Join-Path $PSHOME 'powershell.exe'
}

$openCmd = '"{0}" -NoProfile -ExecutionPolicy Bypass -File "{1}" "%1"' -f $pwsh, $openScript

if ($PSCmdlet.ShouldProcess($installRoot, 'Create install directory and copy scripts')) {
    New-Item -ItemType Directory -Force -Path $installRoot | Out-Null
    Copy-Item -Force -Path (Join-Path $PSScriptRoot 'Open-PassThrough.ps1') -Destination $openScript
    Copy-Item -Force -Path (Join-Path $PSScriptRoot 'PassThrough.Common.ps1') -Destination $commonScript
}

$progPath = "HKCU:\Software\Classes\$script:ProgId"
if ($PSCmdlet.ShouldProcess($progPath, 'Register ProgID')) {
    New-Item -Force -Path $progPath | Out-Null
    Set-ItemProperty -Path $progPath -Name '(default)' -Value $script:FriendlyTypeName
    New-Item -Force -Path "$progPath\shell\open\command" | Out-Null
    Set-ItemProperty -Path "$progPath\shell\open\command" -Name '(default)' -Value $openCmd
    # Generic document icon; OS cannot easily inherit inner DefaultIcon without a shell ext
    New-Item -Force -Path "$progPath\DefaultIcon" | Out-Null
    Set-ItemProperty -Path "$progPath\DefaultIcon" -Name '(default)' -Value 'imageres.dll,-102'
}

foreach ($suffix in $script:PassThroughSuffixes) {
    $ext = '.' + $suffix
    $extPath = "HKCU:\Software\Classes\$ext"
    if ($PSCmdlet.ShouldProcess($extPath, "Map $ext -> $script:ProgId")) {
        New-Item -Force -Path $extPath | Out-Null
        Set-ItemProperty -Path $extPath -Name '(default)' -Value $script:ProgId
        Set-ItemProperty -Path $extPath -Name 'PerceivedType' -Value 'text'
        # Remember we own this mapping for clean uninstall
        New-Item -Force -Path "HKCU:\Software\HCI-Nerdz\PassThrough\Extensions" | Out-Null
        Set-ItemProperty -Path "HKCU:\Software\HCI-Nerdz\PassThrough\Extensions" -Name $suffix -Value $script:ProgId
    }
}

# Notify Explorer of assoc change
Add-Type -Namespace HCINerdz -Name ShellNotify -MemberDefinition @'
[DllImport("shell32.dll")] public static extern void SHChangeNotify(int wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);
'@ -ErrorAction SilentlyContinue
if ([type]::GetType('HCINerdz.ShellNotify')) {
    # SHCNE_ASSOCCHANGED = 0x08000000, SHCNF_IDLIST = 0
    [HCINerdz.ShellNotify]::SHChangeNotify(0x08000000, 0, [IntPtr]::Zero, [IntPtr]::Zero)
}

Write-Host "Pass-through extensions installed for the current user."
Write-Host "  Broker: $openScript"
Write-Host "  Suffixes: $($script:PassThroughSuffixes -join ', ')"
Write-Host "Double-click appsettings.json.example (etc.) to verify."
Write-Host "Uninstall: .\Uninstall-PassThrough.ps1"
