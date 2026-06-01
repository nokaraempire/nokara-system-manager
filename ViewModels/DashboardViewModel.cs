using System.Collections.ObjectModel;
using System.Windows.Input;
using NokaraSystemManager.Helpers;
using NokaraSystemManager.Models;
using NokaraSystemManager.Services;

namespace NokaraSystemManager.ViewModels;

public sealed class DashboardViewModel : ViewModelBase
{
    private readonly AppServices _services;
    private readonly Action<string> _navigate;
    private SystemSummary _summary = new();
    private DetectionSnapshot _snapshot = new();
    private string _previewMessage = "Seleccioná Vista previa para ver qué incluye cada flujo guiado.";

    public DashboardViewModel(AppServices services, Action<string> navigate)
    {
        _services = services;
        _navigate = navigate;
        RefreshCommand = new AsyncRelayCommand(LoadAsync);
        NavigateCommand = new RelayCommand(section => _navigate(section?.ToString() ?? string.Empty));
        OpenProjectCommand = new AsyncRelayCommand(OpenProjectAsync);
        PreviewCommand = new RelayCommand(card =>
        {
            if (card is GuidedActionCard guided)
            {
                PreviewMessage = guided.PreviewText;
            }
        });
    }

    public ICommand RefreshCommand { get; }
    public ICommand NavigateCommand { get; }
    public ICommand OpenProjectCommand { get; }
    public ICommand PreviewCommand { get; }
    public ObservableCollection<ActionHistoryItem> RecentActions { get; } = [];
    public ObservableCollection<RecommendationItem> Recommendations { get; } = [];
    public ObservableCollection<GuidedActionCard> GuidedCards { get; } = [];
    public ObservableCollection<ChecklistItem> GamingChecklist { get; } = [];
    public ObservableCollection<ChecklistItem> StreamingChecklist { get; } = [];

    public SystemSummary Summary
    {
        get => _summary;
        private set => SetProperty(ref _summary, value);
    }

    public DetectionSnapshot Snapshot
    {
        get => _snapshot;
        private set => SetProperty(ref _snapshot, value);
    }

    public string PreviewMessage
    {
        get => _previewMessage;
        private set => SetProperty(ref _previewMessage, value);
    }

    public async Task LoadAsync()
    {
        Summary = await _services.SystemInfo.GetSummaryAsync();
        Snapshot = await _services.Processes.GetSnapshotAsync();
        RecentActions.Clear();
        foreach (var item in (await _services.Backup.GetActionHistoryAsync()).OrderByDescending(h => h.Timestamp).Take(8))
        {
            RecentActions.Add(item);
        }

        BuildRecommendations();
        BuildGuidedCards();
        BuildChecklists();
    }

    private void BuildRecommendations()
    {
        Recommendations.Clear();
        if (!Summary.IsAdmin)
        {
            Recommendations.Add(new RecommendationItem
            {
                Level = RecommendationLevel.Warning,
                Title = "Funciones admin bloqueadas",
                Text = "Ejecutá como administrador para DNS manual, limpieza de Windows Temp y cambios HKLM.",
                TargetSection = "Ajustes",
                SuggestedAction = "Ver ajustes"
            });
        }

        if (Summary.ReversibleChangesCount == 0)
        {
            Recommendations.Add(new RecommendationItem
            {
                Level = RecommendationLevel.Info,
                Title = "Sin cambios reversibles todavía",
                Text = "Antes de ejecutar acciones medias, creá un punto de restauración o conservá backups automáticos.",
                TargetSection = "Backups",
                SuggestedAction = "Abrir backups"
            });
        }

        if (Snapshot.Games.Count == 0)
        {
            Recommendations.Add(new RecommendationItem
            {
                Level = RecommendationLevel.Info,
                Title = "No se detectó juego abierto",
                Text = "Para priorizar procesos, abrí el juego y actualizá Procesos.",
                TargetSection = "Procesos",
                SuggestedAction = "Ver procesos"
            });
        }

        if (Snapshot.WallpaperApps.Count > 0)
        {
            Recommendations.Add(new RecommendationItem
            {
                Level = RecommendationLevel.SuggestedAction,
                Title = "Wallpaper Engine detectado",
                Text = "Para competitivo, pausalo o cerralo manualmente antes de jugar. Nokara no lo cierra automáticamente.",
                TargetSection = "Gaming",
                SuggestedAction = "Ver gaming"
            });
        }

        if (Snapshot.HeavyProcesses.Count > 0)
        {
            Recommendations.Add(new RecommendationItem
            {
                Level = RecommendationLevel.Warning,
                Title = "Procesos pesados en segundo plano",
                Text = $"{Snapshot.HeavyProcesses.Count} proceso(s) superan 500 MB de RAM. Revisalos antes de jugar o iniciar stream.",
                TargetSection = "Procesos",
                SuggestedAction = "Filtrar pesados"
            });
        }

        if (!Summary.CurrentPowerPlan.Contains("Alto", StringComparison.OrdinalIgnoreCase) &&
            !Summary.CurrentPowerPlan.Contains("High", StringComparison.OrdinalIgnoreCase))
        {
            Recommendations.Add(new RecommendationItem
            {
                Level = RecommendationLevel.SuggestedAction,
                Title = "Plan de energía no parece óptimo",
                Text = "Alto rendimiento puede ayudar a evitar bajones por ahorro energético.",
                TargetSection = "Gaming",
                SuggestedAction = "Ver plan"
            });
        }

        if (Recommendations.Count == 0)
        {
            Recommendations.Add(new RecommendationItem
            {
                Level = RecommendationLevel.Info,
                Title = "Sistema listo",
                Text = "No hay alertas obvias. Usá primero acciones de bajo riesgo y revisá backups.",
                TargetSection = "Mantenimiento",
                SuggestedAction = "Mantenimiento rapido"
            });
        }
    }

    private void BuildGuidedCards()
    {
        GuidedCards.Clear();
        GuidedCards.Add(new GuidedActionCard
        {
            Title = "Diagnóstico completo",
            Description = "Stuttering, RAM, almacenamiento, red, streaming, juegos detectados y reporte.",
            IncludedActions = "Sistema, RAM, discos, red, procesos, reporte final",
            RiskLabel = "Bajo",
            TargetSection = "Diagnóstico",
            PreviewText = "Abrí Diagnóstico para analizar el sistema y generar un reporte TXT sin aplicar cambios destructivos."
        });
        GuidedCards.Add(new GuidedActionCard
        {
            Title = "Antes de jugar",
            Description = "Checklist competitivo con Game Mode, energía, overlays y procesos pesados.",
            IncludedActions = "Game Mode, Game DVR, energía, procesos, Wallpaper Engine",
            RiskLabel = "Bajo/Medio",
            TargetSection = "Gaming",
            PreviewText = "Abrí Gaming para aplicar Game Mode, Game DVR OFF, energía alta, mouse y revisar overlays sin cerrar nada automáticamente."
        });
        GuidedCards.Add(new GuidedActionCard
        {
            Title = "Antes de stream",
            Description = "Snapshot liviano para OBS/Streamlabs, voz, red, RAM y apps pesadas.",
            IncludedActions = "OBS, Discord, red activa, RAM, top procesos",
            RiskLabel = "Bajo",
            TargetSection = "Streaming",
            PreviewText = "Abrí Streaming para revisar captura, juego, voz, red y procesos antes de salir en vivo."
        });
        GuidedCards.Add(new GuidedActionCard
        {
            Title = "Mantenimiento rápido",
            Description = "Limpieza segura de temporales, papelera y cachés no personales.",
            IncludedActions = "TEMP usuario, miniaturas, caché DirectX, papelera",
            RiskLabel = "Bajo",
            TargetSection = "Mantenimiento",
            PreviewText = "Abrí Mantenimiento para analizar espacio y ejecutar limpiezas seguras. La caché de Windows Update queda pendiente por seguridad."
        });
        GuidedCards.Add(new GuidedActionCard
        {
            Title = "Reparar internet",
            Description = "Herramientas ordenadas de menor a mayor impacto.",
            IncludedActions = "Flush DNS, benchmark DNS, DNS automático, Winsock/TCP-IP",
            RiskLabel = "Bajo/Avanzado",
            TargetSection = "Red",
            PreviewText = "Empezá por Flush DNS y benchmark. Release/Renew y resets pueden cortar internet o requerir reinicio."
        });
        GuidedCards.Add(new GuidedActionCard
        {
            Title = "Post-formateo",
            Description = "Pendiente: perfil guiado para revisar privacidad, energía y procesos de arranque.",
            IncludedActions = "Privacidad, energía, apps de inicio",
            RiskLabel = "Medio",
            TargetSection = "Perfiles",
            IsImplemented = false,
            DisabledReason = "El perfil post-formateo necesita validaciones adicionales para no aplicar cambios agresivos.",
            PreviewText = "Planeado para V2: asistente paso a paso sin tweaks peligrosos ni servicios críticos."
        });
        GuidedCards.Add(new GuidedActionCard
        {
            Title = "Revertir cambios",
            Description = "Acceso directo a backups de registro, DNS y energía.",
            IncludedActions = "Registro, DNS, energía, historial",
            RiskLabel = "Bajo",
            TargetSection = "Backups",
            PreviewText = "Abrí Backups para restaurar cambios individuales y exportar reportes."
        });
    }

    private void BuildChecklists()
    {
        GamingChecklist.Clear();
        GamingChecklist.Add(Check("Juego detectado", Snapshot.Games.Count > 0, Snapshot.GameStatus));
        GamingChecklist.Add(Check("Plan de energía", Summary.CurrentPowerPlan.Contains("Alto", StringComparison.OrdinalIgnoreCase) || Summary.CurrentPowerPlan.Contains("High", StringComparison.OrdinalIgnoreCase), Summary.CurrentPowerPlan));
        GamingChecklist.Add(Check("Wallpaper Engine", Snapshot.WallpaperApps.Count == 0, Snapshot.WallpaperStatus, warnWhenFalse: true));
        GamingChecklist.Add(Check("Procesos pesados", Snapshot.HeavyProcesses.Count == 0, Snapshot.HeavyProcesses.Count == 0 ? "Sin procesos pesados destacados" : $"{Snapshot.HeavyProcesses.Count} proceso(s) para revisar", warnWhenFalse: true));
        GamingChecklist.Add(Check("Overlays detectados", Snapshot.Overlays.Count == 0, Snapshot.Overlays.Count == 0 ? "Sin overlays destacados" : string.Join(", ", Snapshot.Overlays.Take(3).Select(o => o.DisplayName)), warnWhenFalse: true));

        StreamingChecklist.Clear();
        StreamingChecklist.Add(Check("App de streaming", Snapshot.StreamingApps.Count > 0, Snapshot.StreamingStatus));
        StreamingChecklist.Add(Check("Voz/Discord", Snapshot.VoiceApps.Count > 0, Snapshot.VoiceStatus));
        StreamingChecklist.Add(Check("Red activa", Summary.ConnectionStatus == "Conectado", Summary.ConnectionStatus));
        StreamingChecklist.Add(Check("Espacio libre", Summary.SystemDriveFreeBytes > 10L * 1024 * 1024 * 1024, Summary.SystemDriveFreeDisplay, warnWhenFalse: true));
        StreamingChecklist.Add(Check("Navegadores", Snapshot.Browsers.Count <= 2, Snapshot.BrowserStatus, warnWhenFalse: true));
    }

    private static ChecklistItem Check(string name, bool ok, string detail, bool warnWhenFalse = false)
    {
        return new ChecklistItem
        {
            Name = name,
            Detail = detail,
            State = ok ? ChecklistState.Ok : warnWhenFalse ? ChecklistState.Warning : ChecklistState.Unknown
        };
    }

    private async Task OpenProjectAsync()
    {
        var links = await _services.SocialLinks.LoadAsync();
        SocialLinksService.OpenUrl(string.IsNullOrWhiteSpace(links.Github) ? "https://github.com/nokaraempire" : links.Github);
    }
}
