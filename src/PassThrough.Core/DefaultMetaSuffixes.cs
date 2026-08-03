namespace PassThrough.Core;

/// <summary>Built-in meta-suffixes. Users can add more via settings; they can also disable defaults.</summary>
public static class DefaultMetaSuffixes
{
    public static readonly IReadOnlyList<string> All =
    [
        "example",
        "template",
        "tmpl",
        "sample",
        "dist",
        "default",
        "skeleton",
        "stub",
        "orig",
        "bak",
        "old",
    ];

    /// <summary>Compression / archive tails — never pass-through.</summary>
    public static readonly HashSet<string> CodecTails = new(StringComparer.OrdinalIgnoreCase)
    {
        "gz", "bz2", "xz", "zst", "zip", "7z", "rar",
    };
}
