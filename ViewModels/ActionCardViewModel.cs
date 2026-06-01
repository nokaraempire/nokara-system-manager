using System.Windows;
using System.Windows.Input;
using NokaraSystemManager.Helpers;
using NokaraSystemManager.Models;
using NokaraSystemManager.Services;

namespace NokaraSystemManager.ViewModels;

public sealed class ActionCardViewModel : ViewModelBase
{
    private readonly AppServices _services;
    private readonly Func<OptimizationAction, ActionResult, Task>? _onCompleted;
    private bool _isRunning;
    private string _lastResult = string.Empty;

    public ActionCardViewModel(OptimizationAction action, AppServices services, Func<OptimizationAction, ActionResult, Task>? onCompleted = null)
    {
        Action = action;
        _services = services;
        _onCompleted = onCompleted;
        ExecuteCommand = new AsyncRelayCommand(ExecuteAsync, () => Action.IsEnabled && Action.IsImplemented && !IsRunning);
    }

    public OptimizationAction Action { get; }
    public string Name => Action.Name;
    public string Description => Action.Description;
    public string Details => Action.Details;
    public string WhenToUse => Action.WhenToUse;
    public string ExpectedImpact => Action.ExpectedImpact;
    public string SafetyNotes => Action.SafetyNotes;
    public string CurrentState => Action.CurrentState;
    public string DisabledReason => Action.DisabledReason;
    public string Module => Action.Module;
    public string RiskLabel => Action.RiskLevel switch { RiskLevel.Low => "Bajo", RiskLevel.Medium => "Medio", _ => "Alto" };
    public bool RequiresAdmin => Action.RequiresAdmin;
    public bool IsReversible => Action.IsReversible;
    public bool RequiresRestart => Action.RequiresRestart;
    public bool IsImplemented => Action.IsImplemented;
    public bool IsAdvanced => Action.IsAdvanced;
    public string ModeLabel => IsAdvanced ? "Avanzado" : "Básico";
    public string ExecuteLabel => IsImplemented ? "Ejecutar" : "Pendiente";
    public ICommand ExecuteCommand { get; }

    public bool IsRunning
    {
        get => _isRunning;
        private set => SetProperty(ref _isRunning, value);
    }

    public string LastResult
    {
        get => _lastResult;
        private set => SetProperty(ref _lastResult, value);
    }

    private async Task ExecuteAsync()
    {
        ActionResult result;
        IsRunning = true;
        try
        {
            if (Action.RequiresAdmin && !_services.Admin.IsRunningAsAdmin())
            {
                result = ActionResult.Fail(Action.Name, "Requiere ejecutar como administrador.");
                result.RequiresAdmin = true;
            }
            else if (Action.ExecuteAsync is null)
            {
                result = ActionResult.Fail(Action.Name, string.IsNullOrWhiteSpace(Action.DisabledReason) ? "Acción no disponible." : Action.DisabledReason);
            }
            else if (!await ConfirmIfNeededAsync())
            {
                result = ActionResult.Cancel(Action.Name);
            }
            else
            {
                result = await Action.ExecuteAsync(CancellationToken.None);
            }

            LastResult = result.Success ? result.Message : $"No completado: {result.Message}";
            await _services.Backup.AddActionHistoryAsync(Action.Module, Action.Id, Action.Name, result);
            await _services.Logging.LogActionResultAsync(Action.Module, Action.Name, result);
            if (_onCompleted is not null)
            {
                await _onCompleted(Action, result);
            }
        }
        catch (Exception ex)
        {
            result = ActionResult.Fail(Action.Name, "Error inesperado al ejecutar la acción.", ex);
            LastResult = result.Message;
            await _services.Backup.AddActionHistoryAsync(Action.Module, Action.Id, Action.Name, result);
            await _services.Logging.LogActionResultAsync(Action.Module, Action.Name, result);
            await _services.Logging.LogErrorAsync($"Action failed: {Action.Id}", ex);
            if (_onCompleted is not null)
            {
                await _onCompleted(Action, result);
            }
        }
        finally
        {
            IsRunning = false;
        }
    }

    private async Task<bool> ConfirmIfNeededAsync()
    {
        var settings = await _services.Settings.LoadAsync();
        if (!settings.ExtraConfirmations || Action.RiskLevel == RiskLevel.Low)
        {
            return true;
        }

        var message = string.IsNullOrWhiteSpace(Action.ConfirmationMessage)
            ? $"{Action.Name}\n\n{Action.Description}\n\nRiesgo: {RiskLabel}\nReversible: {(Action.IsReversible ? "Sí" : "No")}\n\n¿Querés ejecutar esta acción?"
            : Action.ConfirmationMessage;

        return MessageBox.Show(message, "Confirmar acción sensible", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;
    }
}
