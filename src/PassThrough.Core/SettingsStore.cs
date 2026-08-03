using System.Text.Json;
using System.Text.Json.Serialization;

namespace PassThrough.Core;

public sealed class PassThroughSettings
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>Extra meta-suffixes the user added (without leading dots).</summary>
    [JsonPropertyName("customSuffixes")]
    public List<string> CustomSuffixes { get; set; } = [];

    /// <summary>Built-in suffixes the user turned off.</summary>
    [JsonPropertyName("disabledDefaults")]
    public List<string> DisabledDefaults { get; set; } = [];

    public IReadOnlyList<string> GetActiveSuffixes()
    {
        var disabled = new HashSet<string>(
            DisabledDefaults.Select(Normalize),
            StringComparer.OrdinalIgnoreCase);

        var result = new List<string>();
        foreach (var s in DefaultMetaSuffixes.All)
        {
            var n = Normalize(s);
            if (n.Length == 0 || disabled.Contains(n)) continue;
            if (DefaultMetaSuffixes.CodecTails.Contains(n)) continue;
            result.Add(n);
        }

        foreach (var s in CustomSuffixes)
        {
            var n = Normalize(s);
            if (n.Length == 0) continue;
            if (DefaultMetaSuffixes.CodecTails.Contains(n)) continue;
            if (result.Exists(x => x.Equals(n, StringComparison.OrdinalIgnoreCase))) continue;
            result.Add(n);
        }

        return result;
    }

    public static string Normalize(string suffix)
    {
        var s = suffix.Trim().TrimStart('.');
        if (s.Contains('.') || s.Contains('/') || s.Contains('\\') || s.Contains(' '))
            return string.Empty;
        return s.ToLowerInvariant();
    }
}

public static class SettingsStore
{
    static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static string GetAppDataDirectory()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "HCI-Nerdz",
            "pass-through-extensions");
        Directory.CreateDirectory(dir);
        return dir;
    }

    public static string GetSettingsPath() =>
        Path.Combine(GetAppDataDirectory(), "settings.json");

    public static PassThroughSettings Load()
    {
        var path = GetSettingsPath();
        if (!File.Exists(path))
            return new PassThroughSettings();

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<PassThroughSettings>(json, JsonOptions)
                   ?? new PassThroughSettings();
        }
        catch
        {
            return new PassThroughSettings();
        }
    }

    public static void Save(PassThroughSettings settings)
    {
        var path = GetSettingsPath();
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(path, json);
    }
}
