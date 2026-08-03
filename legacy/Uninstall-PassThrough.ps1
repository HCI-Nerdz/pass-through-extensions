<#
.SYNOPSIS
  Remove HCI Nerdz pass-through extension registrations for the current user.
#>
[CmdletBinding(SupportsShouldProcess = $true)]
param()

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'PassThrough.Common.ps1')

$extRoot = 'HKCU:\Software\HCI-Nerdz\PassThrough\Extensions'
$suffixes = @($script:PassThroughSuffixes)
if (Test-Path $extRoot) {
    $props = Get-ItemProperty -Path $extRoot -ErrorAction SilentlyContinue
    if ($props) {
        $extra = $props.PSObject.Properties |
            Where-Object { $_.Name -notmatch '^PS' } |
            ForEach-Object { $_.Name }
        $suffixes = @($suffixes + $extra | Select-Object -Unique)
    }
}

foreach ($suffix in $suffixes) {
    $extPath = "HKCU:\Software\Classes\.$suffix"
    if (-not (Test-Path $extPath)) { continue }
    $current = (Get-ItemProperty -Path $extPath -Name '(default)' -ErrorAction SilentlyContinue).'(default)'
    if ($current -eq $script:ProgId) {
        if ($PSCmdlet.ShouldProcess($extPath, 'Remove extension mapping')) {
            Remove-Item -Recurse -Force -Path $extPath
        }
    }
}

$progPath = "HKCU:\Software\Classes\$script:ProgId"
if (Test-Path $progPath) {
    if ($PSCmdlet.ShouldProcess($progPath, 'Remove ProgID')) {
        Remove-Item -Recurse -Force -Path $progPath
    }
}

$meta = 'HKCU:\Software\HCI-Nerdz\PassThrough'
if (Test-Path $meta) {
    if ($PSCmdlet.ShouldProcess($meta, 'Remove install metadata')) {
        Remove-Item -Recurse -Force -Path $meta
    }
}

$installRoot = Get-InstallRoot
if (Test-Path $installRoot) {
    if ($PSCmdlet.ShouldProcess($installRoot, 'Remove installed scripts')) {
        Remove-Item -Recurse -Force -Path $installRoot
    }
}

Add-Type -Namespace HCINerdz -Name ShellNotify -MemberDefinition @'
[DllImport("shell32.dll")] public static extern void SHChangeNotify(int wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);
'@ -ErrorAction SilentlyContinue
if ([type]::GetType('HCINerdz.ShellNotify')) {
    [HCINerdz.ShellNotify]::SHChangeNotify(0x08000000, 0, [IntPtr]::Zero, [IntPtr]::Zero)
}

Write-Host "Pass-through extensions uninstalled for the current user."
