using System.Diagnostics;
using NokaraSystemManager.Models;

namespace NokaraSystemManager.Services;

public sealed class DiagnosticService
{
    private readonly SystemInfoService _systemInfo;
    private readonly ProcessDetectionService _processes;
    private readonly ManualPathsService _manualPaths;
    private readonly CommandRunnerService _commands;
    private readonly LoggingService _logging;

    public DiagnosticService(SystemInfoService systemInfo, ProcessDetectionService processes, ManualPathsService manualPaths, CommandRunnerService commands, LoggingService logging)
    {
        _systemInfo = systemInfo;
        _processes = processes;
        _manualPaths = manualPaths;
        _commands = commands;
        _logging = logging;
    }

    public async Task<DiagnosticSnapshot> AnalyzeAsync()
    {
        var summary = await _systemInfo.GetSummaryAsync();
        var snapshot = await _processes.GetSnapshotAsync();
        var processList = await _processes.GetProcessesAsync();
        var storage = GetStorageInfo();
        var manualPaths = (await _manualPaths.LoadAsync()).ToList();
        var result = new DiagnosticSnapshot
        {
            Summary = summary,
            Processes = snapshot,
            AllProcesses = processList.ToList(),
            Storage = storage,
            ManualPaths = manualPaths
        };

        result.Issues = BuildIssues(result);
        await _logging.LogInfoAsync($"Diagnóstico completo generado. Problemas detectados: {result.Issues.Count}.");
        return result;
    }

    public IReadOnlyList<DiagnosticCard> BuildCards(DiagnosticSnapshot snapshot)
    {
        var ramPercent = snapshot.RamUsagePercent;
        var systemDrive = snapshot.Storage.FirstOrDefault(d => string.Equals(d.Root, Path.GetPathRoot(Environment.SystemDirectory), StringComparison.OrdinalIgnoreCase));
        var stutterIssues = snapshot.Issues.Where(i => i.Title.Contains("stuttering", StringComparison.OrdinalIgnoreCase) ||
                                                       i.Title.Contains("Overlay", StringComparison.OrdinalIgnoreCase) ||
                                                       i.Title.Contains("Wallpaper", StringComparison.OrdinalIgnoreCase) ||
                                                       i.Title.Contains("energía", StringComparison.OrdinalIgnoreCase)).ToList();

        return
        [
            new DiagnosticCard
            {
                Key = "general",
                Title = "Estado general",
                State = snapshot.Issues.Count == 0 ? "Sin alertas fuertes" : $"{snapshot.Issues.Count} punto(s) a revisar",
                Explanation = "Resumen del equipo, permisos, energía, red y estado de la app.",
                Problems = snapshot.Summary.IsAdmin ? "Administrador: Sí" : "Administrador: No. Algunas acciones quedan bloqueadas.",
                SuggestedActions = "Revisá recomendaciones antes de aplicar cambios. Ejecutá como admin solo cuando una acción lo requiera.",
                RiskLabel = "Bajo"
            },
            new DiagnosticCard
            {
                Key = "stuttering",
                Title = "Stuttering / tirones",
                State = stutterIssues.Count == 0 ? "Sin causas obvias" : $"{stutterIssues.Count} causa(s) posible(s)",
                Explanation = "Detecta overlays, Wallpaper Engine, Game DVR, procesos pesados, energía y espacio libre.",
                Problems = stutterIssues.Count == 0 ? "No detectado" : string.Join("; ", stutterIssues.Take(4).Select(i => i.Title)),
                SuggestedActions = "Usá Gaming para Game Mode/Game DVR, revisá overlays y evitá cerrar procesos sin confirmación.",
                RiskLabel = "Bajo/Medio",
                IsReversible = true
            },
            new DiagnosticCard
            {
                Key = "ram",
                Title = "RAM / memoria",
                State = $"{ramPercent:0}% usada",
                Explanation = "Muestra presión de memoria, navegadores, launchers y procesos pesados por RAM.",
                Problems = snapshot.Processes.HeavyProcesses.Count == 0 ? "Sin procesos sobre 500 MB destacados" : string.Join(", ", snapshot.Processes.HeavyProcesses.Take(4).Select(p => $"{p.DisplayName} {p.MemoryMB:0} MB")),
                SuggestedActions = "Detectá procesos pesados. Cerrar o bajar prioridad requiere selección y confirmación.",
                RiskLabel = "Bajo",
                DisabledReason = "Liberar memoria no crítica queda pendiente: requiere implementación segura sin herramientas externas."
            },
            new DiagnosticCard
            {
                Key = "games",
                Title = "Juegos detectados",
                State = snapshot.Processes.Games.Count == 0 ? "No detectado" : $"{snapshot.Processes.Games.Count} abierto(s)",
                Explanation = "Combina procesos conocidos, launchers y rutas manuales configuradas.",
                Problems = snapshot.Processes.Games.Count == 0 ? "No hay juego abierto. Podés agregar ruta manual en Config/manual-paths.json." : string.Join(", ", snapshot.Processes.Games.Take(4).Select(p => p.DisplayName)),
                SuggestedActions = "Para FPS por juego, seleccioná el juego detectado o usá perfil global seguro.",
                RiskLabel = "Bajo/Medio",
                IsReversible = true
            },
            new DiagnosticCard
            {
                Key = "fps",
                Title = "FPS por juego / global",
                State = "Perfil seguro disponible",
                Explanation = "Prioriza cambios globales seguros y evita editar juegos a ciegas.",
                Problems = "No se modifican archivos de juegos sin implementación específica, backup y confirmación.",
                SuggestedActions = "Game Mode, Game DVR, plan de energía, transparencias, overlays y prioridad de proceso seleccionado.",
                RiskLabel = "Bajo/Medio",
                IsReversible = true
            },
            new DiagnosticCard
            {
                Key = "storage",
                Title = "Almacenamiento",
                State = systemDrive is null ? "No detectado" : $"{systemDrive.FreePercent:0}% libre en {systemDrive.Root}",
                Explanation = "Revisa espacio libre y permite comprobación segura con chkdsk /scan.",
                Problems = systemDrive is null ? "No detectado" : systemDrive.FreePercent < 12 ? "Poco espacio libre en unidad del sistema" : "Sin alerta fuerte de espacio",
                SuggestedActions = "Analizá espacio, revisá salud básica y usá Buscar errores solo con /scan.",
                RiskLabel = "Bajo",
                RequiresAdmin = false
            },
            new DiagnosticCard
            {
                Key = "display",
                Title = "Pantalla / GPU",
                State = string.IsNullOrWhiteSpace(snapshot.Summary.GpuDisplay) ? "No detectado" : snapshot.Summary.GpuDisplay,
                Explanation = "Muestra GPU detectada. Resolución/refresco avanzado queda como dato no invasivo.",
                Problems = snapshot.Summary.GpuNames.Count == 0 ? "GPU no detectada por WMI básico" : "Sin alerta directa",
                SuggestedActions = "No se modifican drivers, MSI Mode ni paneles de GPU desde Nokara.",
                RiskLabel = "Bajo"
            },
            new DiagnosticCard
            {
                Key = "network",
                Title = "Red",
                State = snapshot.Summary.ConnectionStatus,
                Explanation = "Muestra adaptador activo, DNS, gateway y acciones seguras de red.",
                Problems = $"DNS: {snapshot.Summary.CurrentDns}",
                SuggestedActions = "Primero Flush DNS y benchmark. Resets de red requieren confirmación.",
                RiskLabel = "Bajo/Medio",
                RequiresAdmin = false
            },
            new DiagnosticCard
            {
                Key = "streaming",
                Title = "Streaming",
                State = snapshot.Processes.StreamingApps.Count == 0 ? "No detectado" : $"{snapshot.Processes.StreamingApps.Count} app(s)",
                Explanation = "Detecta OBS/Streamlabs/TikTok Live Studio, Discord/voz, navegadores y top procesos.",
                Problems = snapshot.Processes.StreamingApps.Count == 0 ? "No hay app de streaming abierta" : string.Join(", ", snapshot.Processes.StreamingApps.Take(3).Select(p => p.DisplayName)),
                SuggestedActions = "No tocar red agresiva durante vivo. Revisá top procesos y mantené Discord estable.",
                RiskLabel = "Bajo"
            },
            new DiagnosticCard
            {
                Key = "recommendations",
                Title = "Recomendaciones",
                State = snapshot.Issues.Count == 0 ? "Sistema listo" : "Acciones sugeridas",
                Explanation = "Prioriza hasta cinco recomendaciones simples y seguras.",
                Problems = snapshot.Issues.Count == 0 ? "No detectado" : string.Join("; ", snapshot.Issues.OrderByDescending(i => i.Severity).Take(5).Select(i => i.SuggestedAction)),
                SuggestedActions = "Generá reporte final antes de aplicar cambios si querés comparar después.",
                RiskLabel = "Bajo"
            },
            new DiagnosticCard
            {
                Key = "report",
                Title = "Reporte final",
                State = "Disponible",
                Explanation = "Crea TXT en Escritorio y copia en AppData\\Reports; abre Notepad automáticamente.",
                Problems = "No aplica cambios. Solo documenta diagnóstico y recomendaciones.",
                SuggestedActions = "Generar reporte final",
                RiskLabel = "Bajo",
                CanApply = true
            }
        ];
    }

    public async Task<ActionResult> RunStorageScanAsync(string root)
    {
        var drive = string.IsNullOrWhiteSpace(root) ? Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\" : root;
        var driveName = drive.TrimEnd('\\');
        if (driveName.Length == 1)
        {
            driveName += ":";
        }

        var result = await _commands.RunAsync("chkdsk.exe", $"{driveName} /scan", 180000);
        await _logging.LogInfoAsync($"chkdsk seguro ejecutado: chkdsk {driveName} /scan");
        return result.Success
            ? ActionResult.Ok("Buscar errores de almacenamiento", "Comprobación /scan completada.", result.CombinedOutput)
            : ActionResult.Fail("Buscar errores de almacenamiento", "La comprobación /scan no finalizó correctamente.", details: result.CombinedOutput);
    }

    private static List<StorageInfoItem> GetStorageInfo()
    {
        var list = new List<StorageInfoItem>();
        foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady))
        {
            list.Add(new StorageInfoItem
            {
                Name = string.IsNullOrWhiteSpace(drive.VolumeLabel) ? drive.Name : drive.VolumeLabel,
                Root = drive.RootDirectory.FullName,
                Type = drive.DriveType.ToString(),
                TotalBytes = drive.TotalSize,
                FreeBytes = drive.AvailableFreeSpace,
                HealthStatus = "No analizado"
            });
        }

        return list;
    }

    private static List<DiagnosticIssue> BuildIssues(DiagnosticSnapshot snapshot)
    {
        var issues = new List<DiagnosticIssue>();
        var ramPercent = snapshot.RamUsagePercent;
        if (ramPercent >= 85)
        {
            issues.Add(new DiagnosticIssue
            {
                Severity = DiagnosticSeverity.Medium,
                Title = "RAM muy cargada",
                Explanation = $"Uso estimado de RAM: {ramPercent:0}%. Puede causar stuttering si el sistema empieza a paginar.",
                SuggestedAction = "Revisar procesos pesados y cerrar manualmente lo no crítico.",
                Risk = RiskLevel.Low
            });
        }

        if (snapshot.Processes.WallpaperApps.Count > 0)
        {
            issues.Add(new DiagnosticIssue
            {
                Severity = DiagnosticSeverity.Low,
                Title = "Wallpaper Engine detectado",
                Explanation = "Puede consumir GPU/CPU en segundo plano durante sesiones competitivas.",
                SuggestedAction = "Pausar o cerrar Wallpaper Engine con confirmación antes de jugar.",
                Risk = RiskLevel.Low,
                IsReversible = true
            });
        }

        if (snapshot.Processes.Overlays.Count > 0)
        {
            issues.Add(new DiagnosticIssue
            {
                Severity = DiagnosticSeverity.Low,
                Title = "Overlays detectados",
                Explanation = string.Join(", ", snapshot.Processes.Overlays.Take(5).Select(p => p.DisplayName)),
                SuggestedAction = "Revisar overlays y desactivar manualmente los que no uses.",
                Risk = RiskLevel.Low
            });
        }

        if (snapshot.Processes.HeavyProcesses.Count > 0)
        {
            issues.Add(new DiagnosticIssue
            {
                Severity = DiagnosticSeverity.Medium,
                Title = "Procesos pesados por RAM",
                Explanation = $"{snapshot.Processes.HeavyProcesses.Count} proceso(s) superan el umbral de RAM.",
                SuggestedAction = "Filtrar Pesados en Procesos y decidir manualmente.",
                Risk = RiskLevel.Low
            });
        }

        if (!snapshot.Summary.CurrentPowerPlan.Contains("Alto", StringComparison.OrdinalIgnoreCase) &&
            !snapshot.Summary.CurrentPowerPlan.Contains("High", StringComparison.OrdinalIgnoreCase) &&
            !snapshot.Summary.CurrentPowerPlan.Contains("Ultimate", StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(new DiagnosticIssue
            {
                Severity = DiagnosticSeverity.Low,
                Title = "Plan de energía no óptimo",
                Explanation = snapshot.Summary.CurrentPowerPlan,
                SuggestedAction = "Usar perfil Gaming para activar Alto rendimiento con backup.",
                Risk = RiskLevel.Medium,
                IsReversible = true
            });
        }

        var systemRoot = Path.GetPathRoot(Environment.SystemDirectory);
        var systemDrive = snapshot.Storage.FirstOrDefault(d => string.Equals(d.Root, systemRoot, StringComparison.OrdinalIgnoreCase));
        if (systemDrive is not null && systemDrive.FreePercent < 12)
        {
            issues.Add(new DiagnosticIssue
            {
                Severity = DiagnosticSeverity.Medium,
                Title = "Poco espacio libre en disco del sistema",
                Explanation = $"{systemDrive.Root} tiene {systemDrive.FreePercent:0}% libre ({systemDrive.FreeDisplay}).",
                SuggestedAction = "Analizar espacio y limpiar temporales seguros.",
                Risk = RiskLevel.Low
            });
        }

        if (!snapshot.Summary.IsAdmin)
        {
            issues.Add(new DiagnosticIssue
            {
                Severity = DiagnosticSeverity.Low,
                Title = "No ejecutado como administrador",
                Explanation = "Las acciones que tocan HKLM, DNS manual o Windows Temp quedan bloqueadas.",
                SuggestedAction = "Reiniciar como admin solo si necesitás esas acciones.",
                Risk = RiskLevel.Low,
                RequiresAdmin = true
            });
        }

        return issues;
    }
}
