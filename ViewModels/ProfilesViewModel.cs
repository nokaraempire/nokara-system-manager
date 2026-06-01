using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using NokaraSystemManager.Helpers;
using NokaraSystemManager.Models;
using NokaraSystemManager.Services;

namespace NokaraSystemManager.ViewModels;

public sealed class ProfilesViewModel : ViewModelBase
{
    private readonly AppServices _services;
    private readonly Func<OptimizationAction, ActionResult, Task> _onCompleted;
    private string _profileName = "Gaming rápido";
    private string _status = "Seleccioná un perfil.";

    public ProfilesViewModel(AppServices services, Func<OptimizationAction, ActionResult, Task> onCompleted)
    {
        _services = services;
        _onCompleted = onCompleted;
        LoadGamingCommand = new RelayCommand(LoadGaming);
        LoadStreamingCommand = new RelayCommand(LoadStreaming);
        LoadRestoreCommand = new RelayCommand(LoadRestore);
        LoadDiagnosticCommand = new RelayCommand(LoadDiagnostic);
        ExecuteProfileCommand = new AsyncRelayCommand(ExecuteProfileAsync, () => Steps.Any(s => s.IsSelected));
        LoadGaming();
    }

    public ObservableCollection<ProfileStep> Steps { get; } = [];
    public ICommand LoadGamingCommand { get; }
    public ICommand LoadStreamingCommand { get; }
    public ICommand LoadRestoreCommand { get; }
    public ICommand LoadDiagnosticCommand { get; }
    public ICommand ExecuteProfileCommand { get; }

    public string ProfileName
    {
        get => _profileName;
        private set => SetProperty(ref _profileName, value);
    }

    public string Status
    {
        get => _status;
        private set => SetProperty(ref _status, value);
    }

    private void LoadGaming()
    {
        ProfileName = "Gaming rápido";
        ReplaceSteps(_services.Profiles.CreateGamingProfile());
    }

    private void LoadStreaming()
    {
        ProfileName = "Streaming estable";
        ReplaceSteps(_services.Profiles.CreateStreamingProfile());
    }

    private void LoadRestore()
    {
        ProfileName = "Restauración segura";
        ReplaceSteps(_services.Profiles.CreateRestoreProfile());
    }

    private void LoadDiagnostic()
    {
        ProfileName = "Diagnóstico completo";
        ReplaceSteps(_services.Profiles.CreateDiagnosticProfile());
    }

    private void ReplaceSteps(IEnumerable<ProfileStep> steps)
    {
        Steps.Clear();
        foreach (var step in steps)
        {
            Steps.Add(step);
        }

        Status = $"{Steps.Count} pasos cargados.";
    }

    private async Task ExecuteProfileAsync()
    {
        var settings = await _services.Settings.LoadAsync();
        if (settings.ExtraConfirmations &&
            MessageBox.Show($"{ProfileName}\n\nSe ejecutarán los pasos seleccionados y se registrarán resultados/backups cuando aplique.\n\n¿Querés continuar?", "Confirmar perfil", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
        {
            var cancel = ActionResult.Cancel(ProfileName);
            await _services.Backup.AddActionHistoryAsync("Perfiles", $"profile.{ProfileName}", ProfileName, cancel);
            await _services.Logging.LogActionResultAsync("Perfiles", ProfileName, cancel);
            Status = "Perfil cancelado por el usuario.";
            return;
        }

        var results = new List<ActionResult>();
        foreach (var step in Steps.Where(s => s.IsSelected).ToList())
        {
            if (step.RequiresAdmin && !_services.Admin.IsRunningAsAdmin())
            {
                var adminResult = ActionResult.Fail(step.Name, "Requiere administrador.");
                adminResult.RequiresAdmin = true;
                results.Add(adminResult);
                continue;
            }

            results.Add(await ExecuteStepAsync(step));
        }

        var combined = ActionResult.Combine(ProfileName, results);
        var action = new OptimizationAction { Id = $"profile.{ProfileName}", Name = ProfileName, Module = "Perfiles", RiskLevel = RiskLevel.Medium };
        await _services.Backup.AddActionHistoryAsync(action.Module, action.Id, action.Name, combined);
        await _services.Logging.LogActionResultAsync(action.Module, action.Name, combined);
        Status = combined.Message;
        await _onCompleted(action, combined);
        var reportPath = await _services.Reports.GenerateSystemReportAsync($"Perfil {ProfileName}", openNotepad: true);
        Status = $"{combined.Message} Reporte: {reportPath}";
    }

    private Task<ActionResult> ExecuteStepAsync(ProfileStep step)
    {
        return step.Id switch
        {
            "gaming.game-mode" => _services.Gaming.EnableGameModeAsync(),
            "gaming.game-dvr" => _services.Gaming.DisableGameDvrAsync(),
            "gaming.power-plan" => _services.Gaming.ActivatePerformancePowerPlanAsync(),
            "gaming.transparency" => _services.Gaming.DisableTransparencyAsync(),
            "network.flush-dns" => _services.Network.FlushDnsAsync(),
            "maintenance.user-temp" => _services.Cleaner.CleanUserTempAsync(),
            "privacy.tips" => _services.Privacy.DisableTipsAsync(),
            "restore.power-plan" => _services.PowerPlan.RestoreLatestAsync(),
            "restore.registry" => Task.FromResult(ActionResult.Ok(step.Name, "Revisá y restaurá cambios individuales desde Backups.")),
            "diagnostic.system" or "diagnostic.processes" or "diagnostic.storage" or "diagnostic.network" => Task.FromResult(ActionResult.Ok(step.Name, "Analizado sin aplicar cambios.")),
            "diagnostic.report" => Task.FromResult(ActionResult.Ok(step.Name, "El reporte final se genera al terminar el perfil.")),
            _ => Task.FromResult(ActionResult.Fail(step.Name, "Paso no implementado."))
        };
    }
}
