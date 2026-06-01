using System.Diagnostics;
using System.Windows.Input;
using NokaraSystemManager.Helpers;
using NokaraSystemManager.Models;
using NokaraSystemManager.Services;

namespace NokaraSystemManager.ViewModels;

public sealed class SettingsViewModel : ViewModelBase
{
    private readonly AppServices _services;
    private AppSettings _settings = new();
    private string _status = "Ajustes listos.";

    public SettingsViewModel(AppServices services)
    {
        _services = services;
        SaveCommand = new AsyncRelayCommand(SaveAsync);
        ResetCommand = new AsyncRelayCommand(ResetAsync);
        OpenDataFolderCommand = new RelayCommand(OpenDataFolder);
        OpenLogsFolderCommand = new RelayCommand(() => OpenFolder(AppPaths.Logs));
        OpenBackupsFolderCommand = new RelayCommand(() => OpenFolder(AppPaths.Backups));
        OpenConfigFolderCommand = new RelayCommand(() => OpenFolder(AppPaths.Config));
        ExportSettingsCommand = new AsyncRelayCommand(ExportSettingsAsync);
        ImportSettingsCommand = new AsyncRelayCommand(ImportSettingsAsync);
    }

    public ICommand SaveCommand { get; }
    public ICommand ResetCommand { get; }
    public ICommand OpenDataFolderCommand { get; }
    public ICommand OpenLogsFolderCommand { get; }
    public ICommand OpenBackupsFolderCommand { get; }
    public ICommand OpenConfigFolderCommand { get; }
    public ICommand ExportSettingsCommand { get; }
    public ICommand ImportSettingsCommand { get; }
    public IReadOnlyList<string> DnsProviderNames { get; } = ["Automatico", "Cloudflare", "Google", "Quad9", "OpenDNS"];
    public string LogsPath => AppPaths.Logs;
    public string BackupsPath => AppPaths.Backups;
    public string ConfigPath => AppPaths.Config;

    public bool ExtraConfirmations
    {
        get => _settings.ExtraConfirmations;
        set
        {
            if (_settings.ExtraConfirmations != value)
            {
                _settings.ExtraConfirmations = value;
                OnPropertyChanged();
            }
        }
    }

    public bool ExpertMode
    {
        get => _settings.ExpertMode;
        set
        {
            if (_settings.ExpertMode != value)
            {
                _settings.ExpertMode = value;
                OnPropertyChanged();
            }
        }
    }

    public string PreferredDnsProvider
    {
        get => _settings.PreferredDnsProvider;
        set
        {
            if (_settings.PreferredDnsProvider != value)
            {
                _settings.PreferredDnsProvider = value;
                OnPropertyChanged();
            }
        }
    }

    public string Status
    {
        get => _status;
        private set => SetProperty(ref _status, value);
    }

    public async Task LoadAsync()
    {
        _settings = await _services.Settings.LoadAsync();
        OnPropertyChanged(nameof(ExtraConfirmations));
        OnPropertyChanged(nameof(ExpertMode));
        OnPropertyChanged(nameof(PreferredDnsProvider));
        Status = "Ajustes cargados.";
    }

    private async Task SaveAsync()
    {
        await _services.Settings.SaveAsync(_settings);
        Status = "Ajustes guardados.";
    }

    private async Task ResetAsync()
    {
        await _services.Settings.ResetAsync();
        await LoadAsync();
        Status = "Ajustes restablecidos.";
    }

    private static void OpenDataFolder()
    {
        AppPaths.EnsureBaseFolders();
        OpenFolder(AppPaths.AppDataRoot);
    }

    private static void OpenFolder(string path)
    {
        AppPaths.EnsureBaseFolders();
        Directory.CreateDirectory(path);
        Process.Start(new ProcessStartInfo("explorer.exe", path) { UseShellExecute = true });
    }

    private async Task ExportSettingsAsync()
    {
        var path = Path.Combine(AppPaths.Config, $"settings-export-{DateTime.Now:yyyyMMdd-HHmmss}.json");
        await _services.Settings.ExportSettingsAsync(path);
        Status = $"Configuración exportada: {path}";
    }

    private async Task ImportSettingsAsync()
    {
        var latest = Directory.Exists(AppPaths.Config)
            ? Directory.GetFiles(AppPaths.Config, "settings-export-*.json").OrderByDescending(File.GetLastWriteTimeUtc).FirstOrDefault()
            : null;

        if (latest is null)
        {
            Status = "Importar pendiente: no se encontró un settings-export-*.json en Config.";
            return;
        }

        _settings = await _services.Settings.ImportSettingsAsync(latest);
        await LoadAsync();
        Status = $"Configuración importada desde {latest}";
    }
}
