<#
.SYNOPSIS
  Smoke-test peel logic without touching the registry.
#>
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'PassThrough.Common.ps1')

$cases = @(
    @{ Path = 'C:\tmp\appsettings.json.example'; ExpectExt = '.json'; ExpectPeel = @('example') }
    @{ Path = 'C:\tmp\.env.example'; ExpectExt = '.env'; ExpectPeel = @('example') }
    @{ Path = 'C:\tmp\nginx.conf.template'; ExpectExt = '.conf'; ExpectPeel = @('template') }
    @{ Path = 'C:\tmp\php.ini.dist'; ExpectExt = '.ini'; ExpectPeel = @('dist') }
    @{ Path = 'C:\tmp\notes.txt.example.bak'; ExpectExt = '.txt'; ExpectPeel = @('bak', 'example') }
    @{ Path = 'C:\tmp\archive.tar.gz'; ExpectExt = '.gz'; ExpectPeel = @() }
    @{ Path = 'C:\tmp\readme.md'; ExpectExt = '.md'; ExpectPeel = @() }
    @{ Path = 'C:\tmp\file.example'; ExpectExt = $null; ExpectPeel = @('example') }
)

$failed = 0
foreach ($c in $cases) {
    $r = Invoke-PassThroughPeel -Path $c.Path
    $peelOk = (($r.Peeled -join ',') -eq ($c.ExpectPeel -join ','))
    $extOk = ($null -eq $c.ExpectExt -and $null -eq $r.InnerExt) -or ($r.InnerExt -eq $c.ExpectExt)
    if ($peelOk -and $extOk) {
        Write-Host "OK   $($c.Path)" -ForegroundColor Green
    }
    else {
        $failed++
        Write-Host "FAIL $($c.Path): got peel=[$($r.Peeled -join ',')] inner=$($r.InnerExt) want peel=[$($c.ExpectPeel -join ',')] inner=$($c.ExpectExt)" -ForegroundColor Red
    }
}

if ($failed -gt 0) {
    Write-Host "$failed case(s) failed." -ForegroundColor Red
    exit 1
}
Write-Host "All peel cases passed." -ForegroundColor Green
exit 0
