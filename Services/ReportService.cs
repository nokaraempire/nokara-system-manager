using System.Diagnostics;
using System.Text;
using NokaraSystemManager.Helpers;
using NokaraSystemManager.Models;

namespace NokaraSystemManager.Services;

public sealed class ReportService
{
    private readonly DiagnosticService _diagnostics;
    private readonly BackupService _backup;
    private readonly LoggingService _logging;

    public ReportService(DiagnosticService diagnostics, BackupService backup, LoggingService logging)
    {
        _diagnostics = diagnostics;
        _backup = backup;
        _logging = logging;
    }

    public async Task<string> GenerateSystemReportAsync(string context, bool openNotepad = true, DiagnosticSnapshot? snapshot = null)
    {
        AppPaths.EnsureBaseFolders();
        snapshot ??= await _diagnostics.AnalyzeAsync();

        var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var fileName = $"Nokara-Reporte-{timestamp}.txt";
        var reportsPath = Path.Combine(AppPaths.Reports, fileName);
        var desktopPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), fileName);
        var content = await BuildReportAsync(context, snapshot);
        await File.WriteAllTextAsync(reportsPath, content, Encoding.UTF8);
        await File.WriteAllTextAsync(desktopPath, content, Encoding.UTF8);
        await _logging.LogInfoAsync($"Reporte generado: {reportsPath}; copia escritorio: {desktopPath}");

        if (openNotepad)
        {
            Process.Start(new ProcessStartInfo("notepad.exe", desktopPath) { UseShellExecute = true });
        }

        return desktopPath;
    }

    private async Task<string> BuildReportAsync(string context, DiagnosticSnapshot snapshot)
    {
        var history = (await _backup.GetActionHistoryAsync()).OrderByDescending(h => h.Timestamp).Take(12).ToList();
        var issuesLow = snapshot.Issues.Where(i => i.Severity == DiagnosticSeverity.Low).ToList();
        var issuesMedium = snapshot.Issues.Where(i => i.Severity == DiagnosticSeverity.Medium).ToList();
        var issuesHigh = snapshot.Issues.Where(i => i.Severity == DiagnosticSeverity.High).ToList();
        var topRecommendations = snapshot.Issues
            .OrderByDescending(i => i.Severity)
            .Select(i => i.SuggestedAction)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct()
            .Take(5)
            .ToList();

        var sb = new StringBuilder();
        AppendHeader(sb, "Nokara System Manager - Reporte");
        sb.AppendLine($"Contexto: {context}");
        sb.AppendLine();

        AppendHeader(sb, "1. Resumen");
        sb.AppendLine($"Fecha: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"Equipo: {snapshot.Summary.MachineName}");
        sb.AppendLine($"Usuario: {snapshot.Summary.UserName}");
        sb.AppendLine($"Windows: {snapshot.Summary.WindowsVersion}");
        sb.AppendLine($"Admin: {(snapshot.Summary.IsAdmin ? "Si" : "No")}");
        sb.AppendLine($"Version app: {snapshot.Summary.AppVersion}");
        sb.AppendLine();

        AppendHeader(sb, "2. Pantalla");
        sb.AppendLine("Resolucion: No detectado");
        sb.AppendLine("Frecuencia: No detectado");
        sb.AppendLine($"GPU: {snapshot.Summary.GpuDisplay}");
        sb.AppendLine("Recomendacion: no se modifican drivers, HPET, MSI Mode ni paneles de GPU.");
        sb.AppendLine();

        AppendHeader(sb, "3. Hardware");
        sb.AppendLine($"CPU: {snapshot.Summary.CpuName}");
        sb.AppendLine($"RAM: {snapshot.Summary.UsedRamDisplay} usada de {snapshot.Summary.TotalRamDisplay} ({snapshot.RamUsagePercent:0}%)");
        var systemDrive = snapshot.Storage.FirstOrDefault(d => string.Equals(d.Root, Path.GetPathRoot(Environment.SystemDirectory), StringComparison.OrdinalIgnoreCase));
        sb.AppendLine($"Disco sistema: {systemDrive?.Root ?? "No detectado"}");
        sb.AppendLine($"Espacio libre: {systemDrive?.FreeDisplay ?? "No detectado"}");
        sb.AppendLine($"Salud almacenamiento: {systemDrive?.HealthStatus ?? "No analizado"}");
        sb.AppendLine();

        AppendHeader(sb, "4. Juego(s)");
        sb.AppendLine($"Juegos detectados: {JoinOrDefault(snapshot.Processes.Games.Select(p => $"{p.DisplayName} ({p.ProcessName})"))}");
        sb.AppendLine($"Rutas manuales: {JoinOrDefault(snapshot.ManualPaths.Select(p => $"{p.DisplayName} - {p.ExePath}"))}");
        sb.AppendLine("Acciones aplicadas: ver seccion Cambios aplicados.");
        sb.AppendLine("Acciones sugeridas: Game Mode, Game DVR OFF, plan de energia, revisar overlays y proceso seleccionado.");
        sb.AppendLine();

        AppendHeader(sb, "5. Streaming");
        sb.AppendLine($"App streaming: {JoinOrDefault(snapshot.Processes.StreamingApps.Select(p => p.DisplayName))}");
        sb.AppendLine($"Discord/voz: {JoinOrDefault(snapshot.Processes.VoiceApps.Select(p => p.DisplayName))}");
        sb.AppendLine($"Navegadores: {JoinOrDefault(snapshot.Processes.Browsers.Select(p => p.DisplayName))}");
        sb.AppendLine("Recomendaciones: no resetear red durante vivo; revisar top procesos si hay tirones.");
        sb.AppendLine();

        AppendHeader(sb, "6. RAM");
        sb.AppendLine($"Uso RAM: {snapshot.RamUsagePercent:0}%");
        sb.AppendLine($"Procesos pesados: {JoinOrDefault(snapshot.Processes.HeavyProcesses.Take(8).Select(p => $"{p.DisplayName} {p.MemoryMB:0} MB"))}");
        sb.AppendLine("Acciones aplicadas: ver historial.");
        sb.AppendLine("Recomendacion: cerrar manualmente procesos no criticos si hay presion de memoria.");
        sb.AppendLine();

        AppendHeader(sb, "7. Red");
        sb.AppendLine($"Adaptador activo: {snapshot.Summary.ActiveNetworkAdapter}");
        sb.AppendLine($"IP: {snapshot.Summary.IpAddress}");
        sb.AppendLine($"Gateway: {snapshot.Summary.Gateway}");
        sb.AppendLine($"DNS actual: {snapshot.Summary.CurrentDns}");
        sb.AppendLine("Acciones aplicadas: ver historial.");
        sb.AppendLine("Problemas detectados: No analizado en profundidad.");
        sb.AppendLine();

        AppendHeader(sb, "8. Almacenamiento");
        foreach (var drive in snapshot.Storage)
        {
            sb.AppendLine($"{drive.Root} - libre {drive.FreeDisplay} de {drive.TotalDisplay} ({drive.FreePercent:0}%) - salud: {drive.HealthStatus} - scan: {drive.ScanStatus}");
        }
        if (snapshot.Storage.Count == 0) sb.AppendLine("No detectado");
        sb.AppendLine("Recomendacion: si chkdsk /scan reporta errores, hacer backup y reparar desde Windows.");
        sb.AppendLine();

        AppendHeader(sb, "9. Cambios aplicados");
        if (history.Count == 0)
        {
            sb.AppendLine("No detectado");
        }
        else
        {
            foreach (var item in history)
            {
                sb.AppendLine($"{item.Timestamp:yyyy-MM-dd HH:mm} - {item.Module} - {item.ActionName} - {(item.Success ? "OK" : item.WasCancelled ? "Cancelado" : "Error")} - reversible: {(item.RequiresAdmin ? "requiere admin" : "segun accion")} - backup: ver Backups si aplica");
            }
        }
        sb.AppendLine();

        AppendHeader(sb, "10. Problemas encontrados");
        AppendIssues(sb, "Bajo", issuesLow);
        AppendIssues(sb, "Medio", issuesMedium);
        AppendIssues(sb, "Alto", issuesHigh);
        sb.AppendLine();

        AppendHeader(sb, "11. Que conviene hacer ahora");
        if (topRecommendations.Count == 0)
        {
            sb.AppendLine("1. Mantener acciones de bajo riesgo y revisar backups antes de cambios medios.");
        }
        else
        {
            for (var i = 0; i < topRecommendations.Count; i++)
            {
                sb.AppendLine($"{i + 1}. {topRecommendations[i]}");
            }
        }
        sb.AppendLine();

        AppendHeader(sb, "12. Notas");
        sb.AppendLine("Nokara System Manager no promete FPS magicos. Ayuda a detectar y aplicar ajustes seguros.");
        sb.AppendLine("No desactiva Defender, Firewall ni Windows Update. No toca anti-cheats, BCDEDIT, HPET, MSI Mode ni drivers.");
        return sb.ToString();
    }

    private static void AppendHeader(StringBuilder sb, string text)
    {
        sb.AppendLine(text);
        sb.AppendLine(new string('-', Math.Min(text.Length, 72)));
    }

    private static void AppendIssues(StringBuilder sb, string title, IReadOnlyList<DiagnosticIssue> issues)
    {
        sb.AppendLine($"{title}:");
        if (issues.Count == 0)
        {
            sb.AppendLine("- No detectado");
            return;
        }

        foreach (var issue in issues)
        {
            sb.AppendLine($"- {issue.Title}: {issue.Explanation} Sugerencia: {issue.SuggestedAction}");
        }
    }

    private static string JoinOrDefault(IEnumerable<string> values)
    {
        var list = values.Where(v => !string.IsNullOrWhiteSpace(v)).Distinct().ToList();
        return list.Count == 0 ? "No detectado" : string.Join(", ", list);
    }
}
