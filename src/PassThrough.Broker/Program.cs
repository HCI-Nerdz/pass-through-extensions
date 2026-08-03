using PassThrough.Core;

if (args.Length < 1 || string.IsNullOrWhiteSpace(args[0]))
{
    Console.Error.WriteLine("Usage: PassThrough.Broker.exe <path>");
    return 1;
}

var path = args[0];
if (!File.Exists(path))
{
    Console.Error.WriteLine($"File not found: {path}");
    return 1;
}

var full = Path.GetFullPath(path);
var settings = SettingsStore.Load();
var active = settings.GetActiveSuffixes();
var peel = Peel.Invoke(full, active);

if (!peel.DidPeel || string.IsNullOrEmpty(peel.InnerExt))
{
    AssociationQuery.LaunchNotepad(full);
    return 0;
}

var template = AssociationQuery.GetOpenCommand(peel.InnerExt);
if (string.IsNullOrWhiteSpace(template) ||
    template.Contains("PassThrough.Broker", StringComparison.OrdinalIgnoreCase) ||
    template.Contains("HCINerdz.PassThrough", StringComparison.OrdinalIgnoreCase))
{
    AssociationQuery.LaunchNotepad(full);
    return 0;
}

AssociationQuery.LaunchCommand(template, full);
return 0;
