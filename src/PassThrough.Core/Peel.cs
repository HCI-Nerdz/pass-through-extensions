namespace PassThrough.Core;

public sealed record PeelResult(
    string Path,
    string BaseName,
    IReadOnlyList<string> Peeled,
    string? InnerExt,
    bool DidPeel);

public static class Peel
{
    public static PeelResult Invoke(string path, IEnumerable<string> activeSuffixes)
    {
        var set = new HashSet<string>(
            activeSuffixes.Select(PassThroughSettings.Normalize).Where(s => s.Length > 0),
            StringComparer.OrdinalIgnoreCase);

        var baseName = System.IO.Path.GetFileName(path);
        var (parts, hiddenStem) = SplitSegments(baseName);
        var working = new List<string>(parts);
        var peeled = new List<string>();

        while (working.Count >= 2)
        {
            var tail = working[^1];
            if (!set.Contains(tail)) break;
            peeled.Add(tail.ToLowerInvariant());
            working.RemoveAt(working.Count - 1);
        }

        string? innerExt = null;
        if (working.Count >= 2)
            innerExt = "." + working[^1];
        else if (working.Count == 1 && hiddenStem && peeled.Count > 0)
            innerExt = "." + working[0];
        else if (working.Count == 1 && peeled.Count == 0 && parts.Count >= 2)
            innerExt = "." + working[0];

        return new PeelResult(path, baseName, peeled, innerExt, peeled.Count > 0);
    }

    static (List<string> Parts, bool HiddenStem) SplitSegments(string baseName)
    {
        if (string.IsNullOrWhiteSpace(baseName))
            return ([], false);

        if (baseName.StartsWith('.') && baseName.LastIndexOf('.') == 0)
            return ([baseName[1..]], true);

        if (baseName.StartsWith('.'))
            return ([.. baseName[1..].Split('.')], true);

        return ([.. baseName.Split('.')], false);
    }
}
