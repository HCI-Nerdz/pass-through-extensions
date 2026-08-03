<#
.SYNOPSIS
  Open a file by peeling pass-through meta-suffixes and launching the inner association.

.DESCRIPTION
  Registered as the open verb for .example / .template / … by Install-PassThrough.ps1.
  Peels allow-listed badges, resolves the stem extension via AssocQueryString, and
  starts that command with the *real* path (badge stays on disk).

.PARAMETER Path
  Full path to the file to open (Explorer passes %1).
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string] $Path
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'PassThrough.Common.ps1')

if (-not (Test-Path -LiteralPath $Path)) {
    Write-Error "File not found: $Path"
    exit 1
}

$full = (Resolve-Path -LiteralPath $Path).Path
$peel = Invoke-PassThroughPeel -Path $full

function Open-WithDefaultTextEditor {
    param([string] $FilePath)
    # Last resort: notepad (always present)
    Start-Process -FilePath "$env:SystemRoot\System32\notepad.exe" -ArgumentList @($FilePath)
}

if (-not $peel.DidPeel -or -not $peel.InnerExt) {
    Write-Warning "No pass-through peel for '$($peel.BaseName)'; opening as text."
    Open-WithDefaultTextEditor -FilePath $full
    exit 0
}

$template = Get-AssociationCommand -Extension $peel.InnerExt
if (-not $template) {
    Write-Warning "No association for $($peel.InnerExt); opening as text."
    Open-WithDefaultTextEditor -FilePath $full
    exit 0
}

# Avoid recursion if the inner association somehow points back at us
if ($template -match 'Open-PassThrough\.ps1' -or $template -match 'HCINerdz\.PassThrough') {
    Write-Warning "Inner association for $($peel.InnerExt) loops to pass-through; using notepad."
    Open-WithDefaultTextEditor -FilePath $full
    exit 0
}

$cmdline = Expand-AssociationCommand -Template $template -Path $full

# Prefer cmd /c for templates that include args; Start-Process alone mishandles some ftypes
$psi = New-Object System.Diagnostics.ProcessStartInfo
$psi.FileName = "$env:SystemRoot\System32\cmd.exe"
$psi.Arguments = '/c ' + $cmdline
$psi.UseShellExecute = $false
$psi.CreateNoWindow = $true
[void][System.Diagnostics.Process]::Start($psi)
