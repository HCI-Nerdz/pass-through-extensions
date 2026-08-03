using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using PassThrough.Core;

namespace PassThrough.App;

public partial class MainWindow : Window
{
    readonly ObservableCollection<DefaultItem> _defaults = [];
    readonly ObservableCollection<string> _customs = [];

    public MainWindow()
    {
        InitializeComponent();
        DefaultsList.ItemsSource = _defaults;
        CustomList.ItemsSource = _customs;
        LoadFromDisk();
    }

    void LoadFromDisk()
    {
        var settings = SettingsStore.Load();
        EnabledCheck.IsChecked = settings.Enabled;

        var disabled = new HashSet<string>(
            settings.DisabledDefaults.Select(PassThroughSettings.Normalize),
            StringComparer.OrdinalIgnoreCase);

        _defaults.Clear();
        foreach (var s in DefaultMetaSuffixes.All)
        {
            _defaults.Add(new DefaultItem
            {
                Suffix = s,
                IsEnabled = !disabled.Contains(s),
            });
        }

        _customs.Clear();
        foreach (var s in settings.CustomSuffixes
                     .Select(PassThroughSettings.Normalize)
                     .Where(s => s.Length > 0)
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            _customs.Add(s);
        }

        StatusText.Text = AssociationRegistrar.IsApplied()
            ? $"Active for this user · settings: {SettingsStore.GetSettingsPath()}"
            : $"Not applied yet · settings will live at {SettingsStore.GetSettingsPath()}";
    }

    PassThroughSettings CaptureSettings()
    {
        return new PassThroughSettings
        {
            Enabled = EnabledCheck.IsChecked == true,
            CustomSuffixes = _customs.ToList(),
            DisabledDefaults = _defaults
                .Where(d => !d.IsEnabled)
                .Select(d => d.Suffix)
                .ToList(),
        };
    }

    void OnAddCustom(object sender, RoutedEventArgs e)
    {
        var n = PassThroughSettings.Normalize(CustomInput.Text);
        if (n.Length == 0)
        {
            StatusText.Text = "Enter a single suffix token (letters/digits), no dot.";
            return;
        }

        if (DefaultMetaSuffixes.CodecTails.Contains(n))
        {
            StatusText.Text = $".{n} is a compression/archive tail — not a pass-through badge.";
            return;
        }

        if (DefaultMetaSuffixes.All.Contains(n, StringComparer.OrdinalIgnoreCase))
        {
            StatusText.Text = $".{n} is already a built-in. Toggle it on the left.";
            return;
        }

        if (_customs.Contains(n, StringComparer.OrdinalIgnoreCase))
        {
            StatusText.Text = $".{n} is already in your list.";
            return;
        }

        _customs.Add(n);
        CustomInput.Clear();
        StatusText.Text = $"Added .{n}. Click Apply to register it with Explorer.";
    }

    void OnRemoveCustom(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string suffix })
            _customs.Remove(suffix);
    }

    void OnApply(object sender, RoutedEventArgs e)
    {
        try
        {
            var settings = CaptureSettings();
            var brokerBesideApp = Path.Combine(AppContext.BaseDirectory, "PassThrough.Broker.exe");
            AssociationRegistrar.Apply(
                settings,
                File.Exists(brokerBesideApp) ? brokerBesideApp : null);
            StatusText.Text =
                $"Applied {settings.GetActiveSuffixes().Count} meta-type(s) for this user. " +
                "Double-click something.json.example to verify.";
        }
        catch (Exception ex)
        {
            StatusText.Text = "Apply failed: " + ex.Message;
        }
    }

    void OnRemove(object sender, RoutedEventArgs e)
    {
        try
        {
            AssociationRegistrar.Remove();
            var settings = CaptureSettings();
            settings.Enabled = false;
            EnabledCheck.IsChecked = false;
            SettingsStore.Save(settings);
            StatusText.Text = "Removed pass-through mappings from Explorer for this user.";
        }
        catch (Exception ex)
        {
            StatusText.Text = "Remove failed: " + ex.Message;
        }
    }
}

public sealed class DefaultItem : INotifyPropertyChanged
{
    bool _isEnabled = true;

    public required string Suffix { get; init; }
    public string Display => "." + Suffix;

    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (_isEnabled == value) return;
            _isEnabled = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
