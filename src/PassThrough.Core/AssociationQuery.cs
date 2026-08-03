using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;

namespace PassThrough.Core;

public static class AssociationQuery
{
    const uint ASSOCSTR_COMMAND = 1;

    [DllImport("shlwapi.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    static extern int AssocQueryStringW(
        uint flags,
        uint str,
        string pszAssoc,
        string? pszExtra,
        StringBuilder pszOut,
        ref uint pcchOut);

    public static string? GetOpenCommand(string extension)
    {
        if (!extension.StartsWith('.'))
            extension = "." + extension;

        var sb = new StringBuilder(2048);
        uint len = (uint)sb.Capacity;
        var hr = AssocQueryStringW(0, ASSOCSTR_COMMAND, extension, null, sb, ref len);
        if (hr == 0 && sb.Length > 0)
            return sb.ToString();

        foreach (var root in new[]
                 {
                     Registry.CurrentUser.OpenSubKey($@"Software\Classes\{extension}"),
                     Registry.ClassesRoot.OpenSubKey(extension),
                 })
        {
            using (root)
            {
                var progId = root?.GetValue(null) as string;
                if (string.IsNullOrWhiteSpace(progId)) continue;
                foreach (var cmdKey in new[]
                         {
                             Registry.CurrentUser.OpenSubKey($@"Software\Classes\{progId}\shell\open\command"),
                             Registry.ClassesRoot.OpenSubKey($@"{progId}\shell\open\command"),
                         })
                {
                    using (cmdKey)
                    {
                        var cmd = cmdKey?.GetValue(null) as string;
                        if (!string.IsNullOrWhiteSpace(cmd))
                            return cmd;
                    }
                }
            }
        }

        return null;
    }

    public static string ExpandCommand(string template, string path)
    {
        var quoted = "\"" + path.Replace("\"", "\"\"") + "\"";
        var result = template
            .Replace("%L", quoted)
            .Replace("%l", quoted)
            .Replace("%1", quoted)
            .Replace("%*", quoted);
        return result;
    }

    public static void LaunchCommand(string template, string path)
    {
        var cmdline = ExpandCommand(template, path);
        var psi = new ProcessStartInfo
        {
            FileName = Path.Combine(Environment.SystemDirectory, "cmd.exe"),
            Arguments = "/c " + cmdline,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        Process.Start(psi);
    }

    public static void LaunchNotepad(string path)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = Path.Combine(Environment.SystemDirectory, "notepad.exe"),
            Arguments = "\"" + path + "\"",
            UseShellExecute = true,
        });
    }
}
