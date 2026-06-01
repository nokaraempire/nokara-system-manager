using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using NokaraSystemManager.Helpers;
using NokaraSystemManager.Models;
using NokaraSystemManager.Services;

namespace NokaraSystemManager.ViewModels;

public sealed class StreamingViewModel : ViewModelBase
{
    private readonly AppServices _services;
    private DetectionSnapshot _snapshot = new();
    private string _recommendation = "Actualizá el snapshot antes de salir en vivo.";

    public StreamingViewModel(AppServices services, Func<OptimizationAction, ActionCardViewModel> cardFactory)
    {
        _services = services;
        RefreshCommand = new AsyncRelayCommand(LoadAsync);
        Actions =
        [
            cardFactory(new OptimizationAction { Id = "streaming.profile", Name = "Perfil Gaming + Streaming", Description = "Prioriza app de streaming y juego detectado sin resetear red ni cerrar procesos.", Module = "Streaming", RiskLevel = RiskLevel.Medium, WhenToUse = "Antes de iniciar OBS/Streamlabs/TikTok Live Studio.", ExpectedImpact = "Prioridad AboveNormal para streaming y High para juego si se detecta. Sin cambios agresivos.", IsAdvanced = true, CurrentState = "Vista previa recomendada", ExecuteAsync = ApplyStreamingProfileAsync }),
            cardFactory(new OptimizationAction { Id = "streaming.snapshot", Name = "Monitor simple de impacto", Description = "Refresca top procesos por RAM, juegos, voz, navegadores y Wallpaper Engine.", Module = "Streaming", RiskLevel = RiskLevel.Low, WhenToUse = "Antes y durante pruebas de stream.", ExpectedImpact = "Solo lectura, liviano.", CurrentState = "Seguro", ExecuteAsync = SnapshotActionAsync }),
            cardFactory(new OptimizationAction { Id = "streaming.game-mode", Name = "Modo juego para streaming", Description = "Activa Game Mode antes de capturar o streamear.", Module = "Streaming", RiskLevel = RiskLevel.Low, IsReversible = true, WhenToUse = "Gaming + stream en la misma PC.", ExpectedImpact = "Windows intenta priorizar la sesión de juego.", CurrentState = "Seguro", ExecuteAsync = services.Gaming.EnableGameModeAsync }),
            cardFactory(new OptimizationAction { Id = "streaming.transparency", Name = "Reducir carga visual", Description = "Desactiva transparencias para liberar algo de GPU/CPU.", Module = "Streaming", RiskLevel = RiskLevel.Low, IsReversible = true, WhenToUse = "Si capturas escritorio o usas escenas pesadas.", ExpectedImpact = "Menos carga visual del entorno.", CurrentState = "Seguro", ExecuteAsync = services.Gaming.DisableTransparencyAsync }),
            cardFactory(new OptimizationAction { Id = "streaming.flush-dns", Name = "Limpiar DNS", Description = "Refresca resolución antes de Twitch, YouTube o Discord.", Module = "Streaming", RiskLevel = RiskLevel.Low, WhenToUse = "Si cambiaste DNS o un servicio no resuelve.", ExpectedImpact = "No corta internet.", CurrentState = "Seguro", ExecuteAsync = services.Network.FlushDnsAsync }),
            cardFactory(new OptimizationAction { Id = "streaming.tips", Name = "Reducir tips de Windows", Description = "Evita sugerencias que pueden interrumpir una sesión.", Module = "Streaming", RiskLevel = RiskLevel.Low, IsReversible = true, WhenToUse = "Antes de capturas largas o presentaciones.", ExpectedImpact = "Menos interrupciones visuales.", CurrentState = "Seguro", ExecuteAsync = services.Privacy.DisableTipsAsync }),
            cardFactory(new OptimizationAction { Id = "streaming.startup", Name = "Revisar apps de inicio", Description = "Pendiente: listar apps que arrancan con Windows y sugerir revisión.", Module = "Streaming", RiskLevel = RiskLevel.Low, IsImplemented = false, IsEnabled = false, WhenToUse = "Cuando muchas apps abren solas antes de stream.", ExpectedImpact = "No se deshabilitará nada sin confirmación.", CurrentState = "Pendiente", DisabledReason = "Pendiente para V2: lectura de Startup/Run con UI de confirmación y backups." })
        ];
    }

    public ObservableCollection<ActionCardViewModel> Actions { get; }
    public ObservableCollection<ChecklistItem> Checklist { get; } = [];
    public ObservableCollection<ProcessInfoItem> TopProcesses { get; } = [];
    public ICommand RefreshCommand { get; }

    public DetectionSnapshot Snapshot
    {
        get => _snapshot;
        private set => SetProperty(ref _snapshot, value);
    }

    public string Recommendation
    {
        get => _recommendation;
        private set => SetProperty(ref _recommendation, value);
    }

    public async Task LoadAsync()
    {
        Snapshot = await _services.Processes.GetSnapshotAsync();
        var summary = await _services.SystemInfo.GetSummaryAsync();
        TopProcesses.Clear();
        foreach (var process in Snapshot.TopProcessesByRam.Take(8))
        {
            TopProcesses.Add(process);
        }

        Checklist.Clear();
        Checklist.Add(new ChecklistItem { Name = "App streaming", State = Snapshot.StreamingApps.Count > 0 ? ChecklistState.Ok : ChecklistState.Unknown, Detail = Snapshot.StreamingStatus });
        Checklist.Add(new ChecklistItem { Name = "Juego", State = Snapshot.Games.Count > 0 ? ChecklistState.Ok : ChecklistState.Unknown, Detail = Snapshot.GameStatus });
        Checklist.Add(new ChecklistItem { Name = "Discord/voz", State = Snapshot.VoiceApps.Count > 0 ? ChecklistState.Ok : ChecklistState.Unknown, Detail = Snapshot.VoiceStatus });
        Checklist.Add(new ChecklistItem { Name = "Red activa", State = summary.ConnectionStatus == "Conectado" ? ChecklistState.Ok : ChecklistState.Warning, Detail = $"{summary.ConnectionStatus} - {summary.ActiveNetworkAdapter}" });
        Checklist.Add(new ChecklistItem { Name = "DNS actual", State = ChecklistState.Unknown, Detail = summary.CurrentDns });
        Checklist.Add(new ChecklistItem { Name = "Disco libre", State = summary.SystemDriveFreeBytes > 10L * 1024 * 1024 * 1024 ? ChecklistState.Ok : ChecklistState.Warning, Detail = summary.SystemDriveFreeDisplay });
        Recommendation = Snapshot.StreamingApps.Count == 0
            ? "Recomendación: abrí OBS/Streamlabs/TikTok Live Studio y actualizá para priorizar correctamente."
            : "Recomendación: evitá reset de red o limpieza de caché de shaders durante stream. Revisá top procesos si hay tirones.";
    }

    private async Task<ActionResult> SnapshotActionAsync(CancellationToken cancellationToken)
    {
        await LoadAsync();
        return ActionResult.Ok("Monitor simple de impacto", $"Snapshot actualizado. Top procesos: {TopProcesses.Count}.", string.Join(Environment.NewLine, TopProcesses.Select(p => $"{p.DisplayName}: {p.MemoryMB} MB")));
    }

    private async Task<ActionResult> ApplyStreamingProfileAsync(CancellationToken cancellationToken)
    {
        var snapshot = await _services.Processes.GetSnapshotAsync();
        var results = new List<ActionResult>
        {
            await _services.Gaming.EnableGameModeAsync(cancellationToken),
            await _services.Gaming.DisableTransparencyAsync(cancellationToken)
        };

        foreach (var process in snapshot.StreamingApps.Take(2))
        {
            results.Add(await _services.ProcessPriority.SetPriorityAsync(process, ProcessPriorityClass.AboveNormal));
        }

        foreach (var process in snapshot.Games.Take(1))
        {
            results.Add(await _services.ProcessPriority.SetPriorityAsync(process, ProcessPriorityClass.High));
        }

        if (snapshot.StreamingApps.Count == 0 && snapshot.Games.Count == 0)
        {
            results.Add(ActionResult.Ok("Prioridades", "No se detectó app de streaming ni juego para priorizar."));
        }

        return ActionResult.Combine("Perfil Gaming + Streaming", results);
    }
}
