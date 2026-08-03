using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace PassThrough.Core;

public static class AssociationRegistrar
{
    public const string ProgId = "HCINerdz.PassThrough";
    public const string FriendlyTypeName = "Pass-through (HCI Nerdz)";
    const string MetaRoot = @"Software\HCI-Nerdz\PassThrough";

    [DllImport("shell32.dll")]
    static extern void SHChangeNotify(int wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

    public static string? FindBrokerPath()
    {
        var appDir = SettingsStore.GetAppDataDirectory();
        var installed = Path.Combine(appDir, "PassThrough.Broker.exe");
        if (File.Exists(installed))
            return installed;

        // Dev: next to the settings app, or under publish output
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "PassThrough.Broker.exe"),
            Path.Combine(AppContext.BaseDirectory, "..", "PassThrough.Broker", "PassThrough.Broker.exe"),
        };
        foreach (var c in candidates)
        {
            var full = Path.GetFullPath(c);
            if (File.Exists(full))
                return full;
        }

        return null;
    }

    public static void EnsureBrokerDeployed(string? sourceBrokerPath = null)
    {
        var destDir = SettingsStore.GetAppDataDirectory();
        Directory.CreateDirectory(destDir);
        var dest = Path.Combine(destDir, "PassThrough.Broker.exe");

        var source = sourceBrokerPath ?? FindBrokerPath();
        if (source is null)
            throw new InvalidOperationException(
                "PassThrough.Broker.exe not found. Build the solution, then Apply again.");

        if (!string.Equals(Path.GetFullPath(source), Path.GetFullPath(dest), StringComparison.OrdinalIgnoreCase))
            File.Copy(source, dest, overwrite: true);

        var srcDir = Path.GetDirectoryName(source)!;
        foreach (var pattern in new[]
                 {
                     "PassThrough.Broker.dll",
                     "PassThrough.Broker.deps.json",
                     "PassThrough.Broker.runtimeconfig.json",
                     "PassThrough.Core.dll",
                     "PassThrough.Core.pdb",
                 })
        {
            var file = Path.Combine(srcDir, pattern);
            if (File.Exists(file))
                File.Copy(file, Path.Combine(destDir, pattern), overwrite: true);
        }
    }

    public static void Apply(PassThroughSettings settings, string? brokerSourcePath = null)
    {
        if (!settings.Enabled)
        {
            Remove();
            return;
        }

        EnsureBrokerDeployed(brokerSourcePath);
        var broker = Path.Combine(SettingsStore.GetAppDataDirectory(), "PassThrough.Broker.exe");
        var openCmd = $"\"{broker}\" \"%1\"";

        using (var prog = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{ProgId}")!)
        {
            prog.SetValue(null, FriendlyTypeName);
            using var cmd = prog.CreateSubKey(@"shell\open\command")!;
            cmd.SetValue(null, openCmd);
            using var icon = prog.CreateSubKey("DefaultIcon")!;
            icon.SetValue(null, @"imageres.dll,-102");
        }

        // Clear previous extension maps we own, then rewrite active set
        var previously = ReadTrackedSuffixes();
        var active = settings.GetActiveSuffixes().ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var old in previously)
        {
            if (!active.Contains(old))
                RemoveExtensionMap(old);
        }

        using (var track = Registry.CurrentUser.CreateSubKey($@"{MetaRoot}\Extensions")!)
        {
            foreach (var name in track.GetValueNames())
                track.DeleteValue(name, throwOnMissingValue: false);

            foreach (var suffix in active)
            {
                MapExtension(suffix);
                track.SetValue(suffix, ProgId);
            }
        }

        SettingsStore.Save(settings);
        NotifyShell();
    }

    public static void Remove()
    {
        foreach (var suffix in ReadTrackedSuffixes().Concat(DefaultMetaSuffixes.All).Distinct(StringComparer.OrdinalIgnoreCase))
            RemoveExtensionMap(suffix);

        try
        {
            Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\{ProgId}", throwOnMissingSubKey: false);
        }
        catch { /* ignore */ }

        try
        {
            Registry.CurrentUser.DeleteSubKeyTree(MetaRoot, throwOnMissingSubKey: false);
        }
        catch { /* ignore */ }

        NotifyShell();
    }

    public static bool IsApplied()
    {
        using var prog = Registry.CurrentUser.OpenSubKey($@"Software\Classes\{ProgId}\shell\open\command");
        return prog?.GetValue(null) is string s && s.Contains("PassThrough.Broker", StringComparison.OrdinalIgnoreCase);
    }

    static void MapExtension(string suffix)
    {
        var ext = "." + suffix;
        using var key = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{ext}")!;
        key.SetValue(null, ProgId);
        key.SetValue("PerceivedType", "text");
    }

    static void RemoveExtensionMap(string suffix)
    {
        var ext = "." + suffix;
        using var key = Registry.CurrentUser.OpenSubKey($@"Software\Classes\{ext}", writable: true);
        if (key is null) return;
        var current = key.GetValue(null) as string;
        if (!string.Equals(current, ProgId, StringComparison.OrdinalIgnoreCase))
            return;
        try
        {
            Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\{ext}", throwOnMissingSubKey: false);
        }
        catch { /* ignore */ }
    }

    static List<string> ReadTrackedSuffixes()
    {
        var list = new List<string>();
        using var track = Registry.CurrentUser.OpenSubKey($@"{MetaRoot}\Extensions");
        if (track is null) return list;
        foreach (var name in track.GetValueNames())
            list.Add(name);
        return list;
    }

    static void NotifyShell()
    {
        // SHCNE_ASSOCCHANGED = 0x08000000
        SHChangeNotify(0x08000000, 0, IntPtr.Zero, IntPtr.Zero);
    }
}
