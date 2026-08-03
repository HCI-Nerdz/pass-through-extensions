# Shared peel logic for HCI Nerdz pass-through extensions.
# Dot-source from Install / Open / Uninstall scripts.

$script:PassThroughSuffixes = @(
    'example'
    'template'
    'tmpl'
    'sample'
    'dist'
    'default'
    'skeleton'
    'stub'
    'orig'
    'bak'
    'old'
)

$script:ProgId = 'HCINerdz.PassThrough'
$script:FriendlyTypeName = 'Pass-through (HCI Nerdz)'

function Get-PassThroughSuffixSet {
    [CmdletBinding()]
    param()
    return [System.Collections.Generic.HashSet[string]]::new(
        [string[]]$script:PassThroughSuffixes,
        [StringComparer]::OrdinalIgnoreCase
    )
}

function Split-FileNameSegments {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string] $FileName
    )

    $base = [System.IO.Path]::GetFileName($FileName)
    if ([string]::IsNullOrWhiteSpace($base)) {
        return [pscustomobject]@{
            Base       = $base
            Parts      = @()
            HiddenStem = $false
        }
    }

    if ($base.StartsWith('.') -and ($base.LastIndexOf('.') -eq 0)) {
        return [pscustomobject]@{
            Base       = $base
            Parts      = @($base.Substring(1))
            HiddenStem = $true
        }
    }

    if ($base.StartsWith('.')) {
        return [pscustomobject]@{
            Base       = $base
            Parts      = @($base.Substring(1).Split('.'))
            HiddenStem = $true
        }
    }

    return [pscustomobject]@{
        Base       = $base
        Parts      = @($base.Split('.'))
        HiddenStem = $false
    }
}

function Invoke-PassThroughPeel {
    <#
    .SYNOPSIS
      Peel allow-listed meta-suffixes from the right; return inner extension + trail.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string] $Path
    )

    $set = Get-PassThroughSuffixSet
    $split = Split-FileNameSegments -FileName $Path
    $parts = [System.Collections.Generic.List[string]]::new()
    foreach ($p in $split.Parts) { [void]$parts.Add($p) }

    $peeled = [System.Collections.Generic.List[string]]::new()
    while ($parts.Count -ge 2) {
        $tail = $parts[$parts.Count - 1]
        if (-not $set.Contains($tail)) { break }
        $peeled.Add($tail.ToLowerInvariant())
        $parts.RemoveAt($parts.Count - 1)
    }

    $innerExt = $null
    if ($parts.Count -ge 2) {
        # Normal stem still has an extension: appsettings.json
        $innerExt = '.' + $parts[$parts.Count - 1]
    }
    elseif ($parts.Count -eq 1 -and $split.HiddenStem -and $peeled.Count -gt 0) {
        # ".env.example" → remaining "env" counts as extension .env
        $innerExt = '.' + $parts[0]
    }
    elseif ($parts.Count -eq 1 -and $peeled.Count -eq 0 -and $split.Parts.Count -ge 2) {
        # No peel (e.g. archive.tar.gz) — last segment is the association key
        $innerExt = '.' + $parts[0]
    }
    elseif ($parts.Count -eq 1 -and $peeled.Count -eq 0 -and -not $split.HiddenStem) {
        # Single-segment name with no dots beyond the name itself — untyped
        $innerExt = $null
    }
    # else: peeled down to a bare stem like "file.example" → no inner type

    return [pscustomobject]@{
        Path       = $Path
        BaseName   = $split.Base
        Peeled     = @($peeled)
        InnerExt   = $innerExt
        DidPeel    = ($peeled.Count -gt 0)
    }
}

function Get-AssociationCommand {
    <#
    .SYNOPSIS
      Resolve the shell open command template for an extension (e.g. '.json').
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string] $Extension
    )

    if (-not $Extension.StartsWith('.')) {
        $Extension = '.' + $Extension
    }

    Add-Type -Namespace HCINerdz -Name AssocNative -MemberDefinition @'
[DllImport("shlwapi.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
public static extern int AssocQueryStringW(
    uint flags,
    uint str,
    string pszAssoc,
    string pszExtra,
    System.Text.StringBuilder pszOut,
    ref uint pcchOut);
'@ -ErrorAction SilentlyContinue

    # ASSOCSTR_COMMAND = 1
    $sb = New-Object System.Text.StringBuilder 2048
    $len = [uint32]$sb.Capacity
    $hr = [HCINerdz.AssocNative]::AssocQueryStringW(0, 1, $Extension, $null, $sb, [ref]$len)
    if ($hr -eq 0 -and $sb.Length -gt 0) {
        return $sb.ToString()
    }

    # Fallback: HKCU / HKCR ProgID open command
    foreach ($root in @(
            "HKCU:\Software\Classes\$Extension",
            "HKCR:\$Extension"
        )) {
        if (-not (Test-Path $root)) { continue }
        $progId = (Get-ItemProperty -Path $root -Name '(default)' -ErrorAction SilentlyContinue).'(default)'
        if (-not $progId) { continue }
        foreach ($cmdRoot in @(
                "HKCU:\Software\Classes\$progId\shell\open\command",
                "HKCR:\$progId\shell\open\command"
            )) {
            if (-not (Test-Path $cmdRoot)) { continue }
            $cmd = (Get-ItemProperty -Path $cmdRoot -Name '(default)' -ErrorAction SilentlyContinue).'(default)'
            if ($cmd) { return $cmd }
        }
    }

    return $null
}

function Expand-AssociationCommand {
    <#
    .SYNOPSIS
      Substitute %1 / %L style placeholders with a quoted path.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string] $Template,
        [Parameter(Mandatory)]
        [string] $Path
    )

    $quoted = '"' + ($Path -replace '"', '""') + '"'
    $result = $Template
    # Longest tokens first
    foreach ($token in @('%L', '%l', '%1', '%*')) {
        if ($token -eq '%*') {
            $result = $result -replace '%\*', $quoted
        }
        else {
            $result = $result.Replace($token, $quoted)
        }
    }
    return $result
}

function Get-InstallRoot {
    $local = Join-Path $env:LOCALAPPDATA 'HCI-Nerdz\pass-through-extensions'
    return $local
}
