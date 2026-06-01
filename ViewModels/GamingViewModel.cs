using System.Collections.ObjectModel;
using System.Windows.Input;
using NokaraSystemManager.Helpers;
using NokaraSystemManager.Models;
using NokaraSystemManager.Services;

namespace NokaraSystemManager.ViewModels;

public sealed class GamingViewModel : ViewModelBase
{
    private readonly AppServices _services;
    private DetectionSnapshot _snapshot = new();
    private string _recommendation = "Actualiza la deteccion para preparar un checklist competitivo.";

    public GamingViewModel(AppServices services, Func<OptimizationAction, ActionCardViewModel> cardFactory)
    {
        _services = services;
        RefreshCommand = new AsyncRelayCommand(LoadAsync);
        Actions =
        [
            cardFactory(new OptimizationAction { Id = "gaming.detect-wallpaper", Name = "Detectar Wallpaper Engine", Description = "Busca wallpaper64.exe / wallpaper32.exe y avisa si conviene pausarlo.", Module = "Gaming", RiskLevel = RiskLevel.Low, WhenToUse = "Antes de competitivo o cuando notas stutter.", ExpectedImpact = "Solo lectura. No cierra procesos.", CurrentState = "Seguro", ExecuteAsync = DetectWallpaperAsync }),
            cardFactory(new OptimizationAction { Id = "gaming.detect-overlays", Name = "Detectar overlays", Description = "Revisa Discord, Steam, Xbox Game Bar, NVIDIA/Radeon y capturadoras.", Module = "Gaming", RiskLevel = RiskLevel.Low, WhenToUse = "Antes de ranked o si sospechas input lag.", ExpectedImpact = "Solo lectura. Muestra sugerencias.", CurrentState = "Seguro", ExecuteAsync = DetectOverlaysAsync }),
            cardFactory(new OptimizationAction { Id = "gaming.detect-heavy", Name = "Detectar procesos pesados", Description = "Lista procesos de alto consumo RAM para revision manual.", Module = "Gaming", RiskLevel = RiskLevel.Low, WhenToUse = "Cuando hay tirones o falta memoria.", ExpectedImpact = "No cierra nada. Ayuda a decidir.", CurrentState = "Seguro", ExecuteAsync = DetectHeavyProcessesAsync }),
            cardFactory(new OptimizationAction { Id = "gaming.profile-fps", Name = "Perfil Competitivo / FPS", Description = "Aplica ajustes seguros: Game Mode, DVR OFF, energia alta, transparencias y mouse opcional.", Module = "Gaming", RiskLevel = RiskLevel.Medium, IsReversible = true, WhenToUse = "Antes de sesiones competitivas.", ExpectedImpact = "Reduce tareas visuales y configuraciones que pueden molestar. No promete FPS magicos.", CurrentState = "Vista previa recomendada", IsAdvanced = true, ExecuteAsync = ApplyCompetitiveProfileAsync }),
            cardFactory(new OptimizationAction { Id = "gaming.game-mode", Name = "Activar Game Mode", Description = "Habilita Game Mode para priorizar juegos compatibles.", Module = "Gaming", RiskLevel = RiskLevel.Low, IsReversible = true, WhenToUse = "Siempre que uses Windows para jugar.", ExpectedImpact = "Windows prioriza experiencia de juego cuando aplica.", CurrentState = "Seguro", ExecuteAsync = services.Gaming.EnableGameModeAsync }),
            cardFactory(new OptimizationAction { Id = "gaming.game-dvr", Name = "Desactivar Game DVR", Description = "Reduce grabacion y capturas en segundo plano.", Module = "Gaming", RiskLevel = RiskLevel.Low, IsReversible = true, WhenToUse = "Si no usas capturas de Xbox/Game Bar.", ExpectedImpact = "Menos overlays y grabacion en segundo plano.", CurrentState = "Seguro", ExecuteAsync = services.Gaming.DisableGameDvrAsync }),
            cardFactory(new OptimizationAction { Id = "gaming.power-plan", Name = "Alto rendimiento", Description = "Activa el plan Alto rendimiento y guarda el anterior.", Module = "Gaming", RiskLevel = RiskLevel.Medium, IsReversible = true, WhenToUse = "PC de escritorio o notebook enchufada.", ExpectedImpact = "Reduce ahorro energetico agresivo. Puede consumir mas.", CurrentState = "Reversible", IsAdvanced = true, ExecuteAsync = services.Gaming.ActivatePerformancePowerPlanAsync }),
            cardFactory(new OptimizationAction { Id = "gaming.transparency", Name = "Reducir transparencias", Description = "Disminuye efectos visuales del escritorio.", Module = "Gaming", RiskLevel = RiskLevel.Low, IsReversible = true, WhenToUse = "Equipos modestos o sesiones de stream.", ExpectedImpact = "Menos carga visual del escritorio.", CurrentState = "Seguro", ExecuteAsync = services.Gaming.DisableTransparencyAsync }),
            cardFactory(new OptimizationAction { Id = "gaming.animations", Name = "Reducir animaciones", Description = "Ajusta animaciones de Windows para una respuesta mas directa.", Module = "Gaming", RiskLevel = RiskLevel.Medium, IsReversible = true, WhenToUse = "Si preferis un escritorio mas rapido y menos animado.", ExpectedImpact = "Cambia la sensacion visual de Windows.", CurrentState = "Confirmacion", IsAdvanced = true, ExecuteAsync = services.Gaming.ReduceAnimationsAsync }),
            cardFactory(new OptimizationAction { Id = "gaming.mouse-acceleration", Name = "Desactivar aceleracion mouse", Description = "Desactiva Enhanced Pointer Precision.", Module = "Gaming", RiskLevel = RiskLevel.Low, IsReversible = true, WhenToUse = "FPS competitivos donde queres input consistente.", ExpectedImpact = "Movimiento mas predecible si jugas con raw input/desactivado.", CurrentState = "Seguro", ExecuteAsync = services.Gaming.DisableMouseAccelerationAsync }),
            cardFactory(new OptimizationAction { Id = "gaming.power-throttling", Name = "Power Throttling off", Description = "Desactiva limitaciones energeticas por registro HKLM.", Module = "Gaming", RiskLevel = RiskLevel.High, RequiresAdmin = true, IsReversible = true, WhenToUse = "Solo si sabes por que lo necesitas.", ExpectedImpact = "Puede cambiar la politica energetica del sistema.", CurrentState = "Avanzado", IsAdvanced = true, ExecuteAsync = services.Gaming.DisablePowerThrottlingAsync }),
            cardFactory(new OptimizationAction { Id = "gaming.usb-suspend", Name = "USB Selective Suspend", Description = "Pendiente: requiere lectura/reversion completa por esquema de energia.", Module = "Gaming", RiskLevel = RiskLevel.High, RequiresAdmin = true, IsImplemented = false, IsEnabled = false, IsAdvanced = true, WhenToUse = "Solo para diagnostico de perifericos USB.", ExpectedImpact = "Puede afectar ahorro energetico USB.", CurrentState = "Pendiente", DisabledReason = "Pendiente en v1.2.0: requiere backup y reversion por plan de energia antes de habilitarse." })
        ];
    }

    public ObservableCollection<ActionCardViewModel> Actions { get; }
    public ObservableCollection<ChecklistItem> Checklist { get; } = [];
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
        Checklist.Clear();
        Checklist.Add(new ChecklistItem { Name = "Juego detectado", State = Snapshot.Games.Count > 0 ? ChecklistState.Ok : ChecklistState.Unknown, Detail = Snapshot.GameStatus });
        Checklist.Add(new ChecklistItem { Name = "Wallpaper Engine", State = Snapshot.WallpaperApps.Count == 0 ? ChecklistState.Ok : ChecklistState.Warning, Detail = Snapshot.WallpaperStatus });
        Checklist.Add(new ChecklistItem { Name = "Overlays", State = Snapshot.Overlays.Count == 0 ? ChecklistState.Ok : ChecklistState.Warning, Detail = Snapshot.Overlays.Count == 0 ? "Sin overlays destacados" : string.Join(", ", Snapshot.Overlays.Take(4).Select(o => o.DisplayName)) });
        Checklist.Add(new ChecklistItem { Name = "Procesos pesados", State = Snapshot.HeavyProcesses.Count == 0 ? ChecklistState.Ok : ChecklistState.Warning, Detail = Snapshot.HeavyProcesses.Count == 0 ? "Sin procesos sobre 500 MB" : $"{Snapshot.HeavyProcesses.Count} proceso(s) para revisar" });
        Checklist.Add(new ChecklistItem { Name = "Streaming abierto", State = Snapshot.StreamingApps.Count == 0 ? ChecklistState.Ok : ChecklistState.Warning, Detail = Snapshot.StreamingStatus });
        Recommendation = Snapshot.WallpaperApps.Count > 0 || Snapshot.HeavyProcesses.Count > 0
            ? "Recomendacion: revisa procesos y overlays antes de aplicar perfiles. Nokara no cierra nada automaticamente."
            : "Recomendacion: listo para aplicar acciones seguras como Game Mode o Game DVR OFF.";
    }

    private async Task<ActionResult> DetectWallpaperAsync(CancellationToken cancellationToken)
    {
        var snapshot = await _services.Processes.GetSnapshotAsync();
        var message = snapshot.WallpaperApps.Count == 0 ? "Wallpaper Engine no esta abierto." : "Wallpaper Engine detectado. Pausalo/cerralo manualmente si buscas FPS estables.";
        return ActionResult.Ok("Detectar Wallpaper Engine", message);
    }

    private async Task<ActionResult> DetectOverlaysAsync(CancellationToken cancellationToken)
    {
        var snapshot = await _services.Processes.GetSnapshotAsync();
        var details = snapshot.Overlays.Count == 0 ? "Sin overlays destacados." : string.Join(Environment.NewLine, snapshot.Overlays.Select(o => $"{o.DisplayName} ({o.ProcessName})"));
        return ActionResult.Ok("Detectar overlays", details == "Sin overlays destacados." ? details : "Overlays detectados. Revisalos antes de competitivo.", details);
    }

    private async Task<ActionResult> DetectHeavyProcessesAsync(CancellationToken cancellationToken)
    {
        var snapshot = await _services.Processes.GetSnapshotAsync();
        var details = snapshot.HeavyProcesses.Count == 0 ? "Sin procesos pesados destacados." : string.Join(Environment.NewLine, snapshot.HeavyProcesses.Select(p => $"{p.DisplayName}: {p.MemoryMB} MB - {p.RecommendedAction}"));
        return ActionResult.Ok("Detectar procesos pesados", snapshot.HeavyProcesses.Count == 0 ? details : "Hay procesos pesados para revisar manualmente.", details);
    }

    private async Task<ActionResult> ApplyCompetitiveProfileAsync(CancellationToken cancellationToken)
    {
        var results = new[]
        {
            await _services.Gaming.EnableGameModeAsync(cancellationToken),
            await _services.Gaming.DisableGameDvrAsync(cancellationToken),
            await _services.Gaming.ActivatePerformancePowerPlanAsync(cancellationToken),
            await _services.Gaming.DisableTransparencyAsync(cancellationToken),
            await _services.Gaming.DisableMouseAccelerationAsync(cancellationToken)
        };
        return ActionResult.Combine("Perfil Competitivo / FPS", results);
    }
}
