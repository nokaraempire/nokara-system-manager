using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using NokaraSystemManager.Helpers;
using NokaraSystemManager.Models;
using NokaraSystemManager.Services;

namespace NokaraSystemManager.ViewModels;

public sealed class BackupRestoreViewModel : ViewModelBase
{
    private readonly AppServices _services;
    private readonly Func<OptimizationAction, ActionResult, Task> _onCompleted;
    private RegistryBackupItem? _selectedRegistryBackup;
    private DnsBackupItem? _selectedDnsBackup;
    private PowerPlanBackupItem? _selectedPowerPlanBackup;
    private string _status = "Backups listos.";

    public BackupRestoreViewModel(AppServices services, Func<OptimizationAction, ActionResult, Task> onCompleted)
    {
        _services = services;
        _onCompleted = onCompleted;
        RefreshCommand = new AsyncRelayCommand(LoadAsync);
        RestoreRegistryCommand = new AsyncRelayCommand(RestoreRegistryAsync, () => SelectedRegistryBackup is not null);
        RestoreDnsCommand = new AsyncRelayCommand(RestoreDnsAsync, () => SelectedDnsBackup is not null);
        RestorePowerPlanCommand = new AsyncRelayCommand(RestorePowerPlanAsync, () => SelectedPowerPlanBackup is not null);
        RestoreAllCommand = new AsyncRelayCommand(RestoreAllAsync);
        OpenBackupsFolderCommand = new RelayCommand(OpenBackupsFolder);
        CreateRestorePointCommand = new AsyncRelayCommand(CreateRestorePointAsync);
    }

    public ObservableCollection<RegistryBackupItem> RegistryBackups { get; } = [];
    public ObservableCollection<DnsBackupItem> DnsBackups { get; } = [];
    public ObservableCollection<PowerPlanBackupItem> PowerPlanBackups { get; } = [];
    public ObservableCollection<ActionHistoryItem> History { get; } = [];
    public ICommand RefreshCommand { get; }
    public ICommand RestoreRegistryCommand { get; }
    public ICommand RestoreDnsCommand { get; }
    public ICommand RestorePowerPlanCommand { get; }
    public ICommand RestoreAllCommand { get; }
    public ICommand OpenBackupsFolderCommand { get; }
    public ICommand CreateRestorePointCommand { get; }

    public RegistryBackupItem? SelectedRegistryBackup
    {
        get => _selectedRegistryBackup;
        set => SetProperty(ref _selectedRegistryBackup, value);
    }

    public DnsBackupItem? SelectedDnsBackup
    {
        get => _selectedDnsBackup;
        set => SetProperty(ref _selectedDnsBackup, value);
    }

    public PowerPlanBackupItem? SelectedPowerPlanBackup
    {
        get => _selectedPowerPlanBackup;
        set => SetProperty(ref _selectedPowerPlanBackup, value);
    }

    public string Status
    {
        get => _status;
        private set => SetProperty(ref _status, value);
    }

    public async Task LoadAsync()
    {
        RegistryBackups.Clear();
        foreach (var item in (await _services.Backup.GetRegistryBackupsAsync()).OrderByDescending(i => i.Timestamp))
        {
            RegistryBackups.Add(item);
        }

        DnsBackups.Clear();
        foreach (var item in (await _services.Backup.GetDnsBackupsAsync()).OrderByDescending(i => i.Timestamp))
        {
            DnsBackups.Add(item);
        }

        PowerPlanBackups.Clear();
        foreach (var item in (await _services.Backup.GetPowerPlanBackupsAsync()).OrderByDescending(i => i.Timestamp))
        {
            PowerPlanBackups.Add(item);
        }

        History.Clear();
        foreach (var item in (await _services.Backup.GetActionHistoryAsync()).OrderByDescending(i => i.Timestamp).Take(100))
        {
            History.Add(item);
        }

        SelectedRegistryBackup ??= RegistryBackups.FirstOrDefault();
        SelectedDnsBackup ??= DnsBackups.FirstOrDefault();
        SelectedPowerPlanBackup ??= PowerPlanBackups.FirstOrDefault();
        Status = "Backups actualizados.";
    }

    private async Task RestoreRegistryAsync()
    {
        if (SelectedRegistryBackup is null) return;
        await RunAsync("restore.registry", "Restaurar registro", () => _services.Registry.RestoreAsync(SelectedRegistryBackup));
        await LoadAsync();
    }

    private async Task RestoreDnsAsync()
    {
        if (SelectedDnsBackup is null) return;
        await RunAsync("restore.dns", "Restaurar DNS", () => _services.Network.RestoreDnsBackupAsync(SelectedDnsBackup));
        await LoadAsync();
    }

    private async Task RestorePowerPlanAsync()
    {
        await RunAsync("restore.power-plan", "Restaurar plan energía", () => _services.PowerPlan.RestoreLatestAsync());
        await LoadAsync();
    }

    private async Task CreateRestorePointAsync()
    {
        await RunAsync("restore.point", "Crear punto restauración", () => _services.RestorePoints.CreateRestorePointAsync(), requiresAdmin: true);
    }

    private async Task RestoreAllAsync()
    {
        var settings = await _services.Settings.LoadAsync();
        if (settings.ExtraConfirmations &&
            MessageBox.Show("Revertir todo intentará restaurar backups pendientes de registro, DNS y energía.\n\n¿Querés continuar?", "Confirmar reversión", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
        {
            var cancel = ActionResult.Cancel("Revertir todo");
            await _services.Backup.AddActionHistoryAsync("Backups", "restore.all", "Revertir todo", cancel);
            await _services.Logging.LogActionResultAsync("Backups", "Revertir todo", cancel);
            Status = "Reversión cancelada por el usuario.";
            return;
        }

        var results = new List<ActionResult>();
        foreach (var item in RegistryBackups.Where(b => b.CanRestore && !b.IsRestored).ToList())
        {
            results.Add(await _services.Registry.RestoreAsync(item));
        }

        foreach (var item in DnsBackups.Where(b => b.CanRestore && !b.IsRestored).ToList())
        {
            results.Add(await _services.Network.RestoreDnsBackupAsync(item));
        }

        if (PowerPlanBackups.Any(b => b.CanRestore && !b.IsRestored))
        {
            results.Add(await _services.PowerPlan.RestoreLatestAsync());
        }

        if (results.Count == 0)
        {
            results.Add(ActionResult.Ok("Revertir todo", "No hay cambios reversibles pendientes."));
        }

        var result = ActionResult.Combine("Revertir todo", results);
        var action = new OptimizationAction { Id = "restore.all", Name = "Revertir todo", Module = "Backups", RiskLevel = RiskLevel.Medium };
        await _services.Backup.AddActionHistoryAsync(action.Module, action.Id, action.Name, result);
        await _services.Logging.LogActionResultAsync(action.Module, action.Name, result);
        Status = result.Message;
        await _onCompleted(action, result);
        await LoadAsync();
    }

    private static void OpenBackupsFolder()
    {
        AppPaths.EnsureBaseFolders();
        Process.Start(new ProcessStartInfo("explorer.exe", AppPaths.Backups) { UseShellExecute = true });
    }

    private async Task RunAsync(string id, string name, Func<Task<ActionResult>> execute, bool requiresAdmin = false)
    {
        var action = new OptimizationAction { Id = id, Name = name, Module = "Backups", RiskLevel = RiskLevel.Medium, RequiresAdmin = requiresAdmin };
        ActionResult result;
        if (requiresAdmin && !_services.Admin.IsRunningAsAdmin())
        {
            result = ActionResult.Fail(name, "Requiere ejecutar como administrador.");
            result.RequiresAdmin = true;
        }
        else
        {
            result = await execute();
        }

        Status = result.Message;
        await _services.Backup.AddActionHistoryAsync(action.Module, action.Id, action.Name, result);
        await _services.Logging.LogActionResultAsync(action.Module, action.Name, result);
        await _onCompleted(action, result);
    }
}
