using System.Diagnostics;
using System.Text.Json.Serialization;
using Microsoft.Win32;
using NokaraSystemManager.Helpers;

namespace NokaraSystemManager.Models;

public enum RiskLevel { Low, Medium, High }
public enum ProcessCategory { Game, GameLauncher, Streaming, Communication, Browser, Multimedia, BackgroundHeavy, System, Custom, Unknown }
public enum RecommendationLevel { Info, Warning, SuggestedAction }
public enum ChecklistState { Ok, Warning, Blocked, Unknown }
public enum DiagnosticSeverity { Low, Medium, High }

public sealed class OptimizationAction
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public RiskLevel RiskLevel { get; set; }
    public bool RequiresAdmin { get; set; }
    public bool IsReversible { get; set; }
    public bool RequiresRestart { get; set; }
    public bool IsEnabled { get; set; } = true;
    public bool IsImplemented { get; set; } = true;
    public bool IsAdvanced { get; set; }
    public string Details { get; set; } = string.Empty;
    public string WhenToUse { get; set; } = string.Empty;
    public string ExpectedImpact { get; set; } = string.Empty;
    public string SafetyNotes { get; set; } = string.Empty;
    public string CurrentState { get; set; } = "Listo";
    public string DisabledReason { get; set; } = string.Empty;
    public string ConfirmationMessage { get; set; } = string.Empty;

    [JsonIgnore]
    public Func<CancellationToken, Task<ActionResult>>? ExecuteAsync { get; set; }
}

public sealed class ActionResult
{
    public bool Success { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public string? Error { get; set; }
    public long SpaceFreedBytes { get; set; }
    public bool RequiresRestart { get; set; }
    public bool RequiresAdmin { get; set; }
    public bool WasCancelled { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.Now;

    public static ActionResult Ok(string title, string message, string details = "", long spaceFreedBytes = 0, bool requiresRestart = false)
    {
        return new ActionResult
        {
            Success = true,
            Title = title,
            Message = message,
            Details = details,
            SpaceFreedBytes = spaceFreedBytes,
            RequiresRestart = requiresRestart
        };
    }

    public static ActionResult Fail(string title, string message, Exception? exception = null, string details = "")
    {
        return new ActionResult { Success = false, Title = title, Message = message, Details = details, Error = exception?.ToString() };
    }

    public static ActionResult Cancel(string title, string message = "Acción cancelada por el usuario.")
    {
        return new ActionResult { Success = false, Title = title, Message = message, WasCancelled = true };
    }

    public static ActionResult Combine(string title, IEnumerable<ActionResult> results)
    {
        var list = results.ToList();
        var success = list.All(r => r.Success);
        return new ActionResult
        {
            Success = success,
            Title = title,
            Message = success ? "Completado." : "Completado parcialmente. Revisa detalles y logs.",
            Details = string.Join(Environment.NewLine, list.Select(r => $"{(r.Success ? "OK" : r.WasCancelled ? "CANCELADO" : "ERROR")} - {r.Title}: {r.Message}")),
            SpaceFreedBytes = list.Sum(r => r.SpaceFreedBytes),
            RequiresRestart = list.Any(r => r.RequiresRestart),
            RequiresAdmin = list.Any(r => r.RequiresAdmin),
            WasCancelled = list.Any(r => r.WasCancelled),
            Error = string.Join(Environment.NewLine, list.Where(r => !string.IsNullOrWhiteSpace(r.Error)).Select(r => r.Error))
        };
    }
}

public sealed class CommandResult
{
    public bool Success => ExitCode == 0 && !TimedOut;
    public int ExitCode { get; set; }
    public string StandardOutput { get; set; } = string.Empty;
    public string StandardError { get; set; } = string.Empty;
    public bool TimedOut { get; set; }
    public TimeSpan Duration { get; set; }
    public string CombinedOutput => string.Join(Environment.NewLine, new[] { StandardOutput, StandardError }.Where(s => !string.IsNullOrWhiteSpace(s)));
}

public sealed class RegistryBackupItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.Now;
    public string Module { get; set; } = string.Empty;
    public string ActionId { get; set; } = string.Empty;
    public string ActionName { get; set; } = string.Empty;
    public RegistryHive Hive { get; set; }
    public string Path { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? PreviousValueJson { get; set; }
    public RegistryValueKind PreviousValueKind { get; set; } = RegistryValueKind.Unknown;
    public string? NewValueJson { get; set; }
    public RegistryValueKind NewValueKind { get; set; } = RegistryValueKind.Unknown;
    public bool ExistedBefore { get; set; }
    public bool CanRestore { get; set; } = true;
    public bool IsRestored { get; set; }
    public string Result { get; set; } = string.Empty;
    public string Target => $"{Hive}\\{Path}\\{Name}";
}

public sealed class RegistrySerializedValue
{
    public RegistryValueKind Kind { get; set; }
    public string? StringValue { get; set; }
    public string[]? StringArrayValue { get; set; }
    public int? DwordValue { get; set; }
    public long? QwordValue { get; set; }
    public string? BinaryBase64 { get; set; }
}

public sealed class DnsBackupItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.Now;
    public string AdapterId { get; set; } = string.Empty;
    public string AdapterName { get; set; } = string.Empty;
    public List<string> PreviousDns { get; set; } = [];
    public List<string> NewDns { get; set; } = [];
    public bool CanRestore { get; set; } = true;
    public bool IsRestored { get; set; }
    public string PreviousDnsDisplay => PreviousDns.Count == 0 ? "Automático/DHCP" : string.Join(", ", PreviousDns);
}

public sealed class PowerPlanBackupItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.Now;
    public string PreviousPlanGuid { get; set; } = string.Empty;
    public string PreviousPlanName { get; set; } = string.Empty;
    public string NewPlanGuid { get; set; } = string.Empty;
    public string NewPlanName { get; set; } = string.Empty;
    public bool CanRestore { get; set; } = true;
    public bool IsRestored { get; set; }
}

public sealed class ActionHistoryItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.Now;
    public string Module { get; set; } = string.Empty;
    public string ActionId { get; set; } = string.Empty;
    public string ActionName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public string? Error { get; set; }
    public long SpaceFreedBytes { get; set; }
    public bool RequiresRestart { get; set; }
    public bool RequiresAdmin { get; set; }
    public bool WasCancelled { get; set; }
}

public sealed class DnsProvider
{
    public string Name { get; set; } = string.Empty;
    public string Primary { get; set; } = string.Empty;
    public string Secondary { get; set; } = string.Empty;
    public override string ToString() => string.IsNullOrWhiteSpace(Primary) ? Name : $"{Name} ({Primary}, {Secondary})";
}

public sealed class DnsTestResult
{
    public string ProviderName { get; set; } = string.Empty;
    public string Primary { get; set; } = string.Empty;
    public string Secondary { get; set; } = string.Empty;
    public double AverageLatencyMs { get; set; }
    public double BestLatencyMs { get; set; }
    public double WorstLatencyMs { get; set; }
    public int Failures { get; set; }
    public int Successes { get; set; }
    public double Score { get; set; }
    public bool IsRecommended { get; set; }
    public string Result => Successes == 0 ? "Sin respuesta" : IsRecommended ? "Recomendado" : "OK";
}

public sealed class ProcessInfoItem
{
    public string ProcessName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int Pid { get; set; }
    public ProcessCategory Category { get; set; } = ProcessCategory.Unknown;
    public ProcessPriorityClass? Priority { get; set; }
    public double MemoryMB { get; set; }
    public bool IsKnown { get; set; }
    public bool IsCritical { get; set; }
    public bool CanChangePriority { get; set; }
    public string Notes { get; set; } = string.Empty;
    public string RecommendedAction { get; set; } = string.Empty;
    public bool IsHighMemory { get; set; }
    public double CpuPercent { get; set; }
}

public sealed class KnownProcessDefinition
{
    public string ProcessName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public ProcessCategory Category { get; set; } = ProcessCategory.Unknown;
    public string Notes { get; set; } = string.Empty;
    public string DefaultPriorityRecommendation { get; set; } = string.Empty;
}

public sealed class SystemSummary
{
    public string MachineName { get; set; } = Environment.MachineName;
    public string UserName { get; set; } = Environment.UserName;
    public string WindowsVersion { get; set; } = string.Empty;
    public string Architecture { get; set; } = string.Empty;
    public string AppVersion { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public string CpuName { get; set; } = string.Empty;
    public long TotalRamBytes { get; set; }
    public long UsedRamBytes { get; set; }
    public List<string> GpuNames { get; set; } = [];
    public long SystemDriveFreeBytes { get; set; }
    public string ActiveNetworkAdapter { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string Gateway { get; set; } = string.Empty;
    public string CurrentDns { get; set; } = string.Empty;
    public string ConnectionStatus { get; set; } = "No verificado";
    public string CurrentPowerPlan { get; set; } = string.Empty;
    public string LastCleanup { get; set; } = "Nunca";
    public string LastGamingOptimization { get; set; } = "Nunca";
    public string LastAction { get; set; } = "Ninguna";
    public int ReversibleChangesCount { get; set; }
    public string RestorePointStatus { get; set; } = "No verificado";
    public string TotalRamDisplay => FormatHelper.Bytes(TotalRamBytes);
    public string UsedRamDisplay => FormatHelper.Bytes(UsedRamBytes);
    public string SystemDriveFreeDisplay => FormatHelper.Bytes(SystemDriveFreeBytes);
    public string GpuDisplay => GpuNames.Count == 0 ? "No detectada" : string.Join(", ", GpuNames);
}

public sealed class NetworkAdapterInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string Gateway { get; set; } = string.Empty;
    public List<string> DnsServers { get; set; } = [];
    public bool IsActive { get; set; }
    public string DnsDisplay => DnsServers.Count == 0 ? "Automático o no detectado" : string.Join(", ", DnsServers);
    public override string ToString() => $"{Name} - {IpAddress}";
}

public sealed class CleanupEstimateItem
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public long Bytes { get; set; }
    public string SizeDisplay { get; set; } = string.Empty;
    public bool RequiresAdmin { get; set; }
}

public sealed class AppSettings
{
    public bool ExtraConfirmations { get; set; } = true;
    public bool ExpertMode { get; set; }
    public string PreferredDnsProvider { get; set; } = "Automatico";
    public DateTimeOffset? LastCleanup { get; set; }
    public DateTimeOffset? LastGamingOptimization { get; set; }
}

public sealed class SocialLinks
{
    public string Github { get; set; } = string.Empty;
    public string Instagram { get; set; } = string.Empty;
    public string Tiktok { get; set; } = string.Empty;
    public string Youtube { get; set; } = string.Empty;
    public string Discord { get; set; } = string.Empty;
    public string Website { get; set; } = string.Empty;
    public string Issues { get; set; } = string.Empty;
}

public sealed class SocialLinkItem
{
    public string Label { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string ButtonText => string.IsNullOrWhiteSpace(Url) ? $"{Label} - Proximamente" : Label;
    public bool IsEnabled => !string.IsNullOrWhiteSpace(Url);
}

public sealed class ManualGamePath
{
    public string DisplayName { get; set; } = string.Empty;
    public string ExePath { get; set; } = string.Empty;
    public string InstallFolder { get; set; } = string.Empty;
    public string Launcher { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public DateTimeOffset AddedAt { get; set; } = DateTimeOffset.Now;
}

public sealed class StorageInfoItem
{
    public string Name { get; set; } = string.Empty;
    public string Root { get; set; } = string.Empty;
    public string Type { get; set; } = "No detectado";
    public long TotalBytes { get; set; }
    public long FreeBytes { get; set; }
    public string TotalDisplay => FormatHelper.Bytes(TotalBytes);
    public string FreeDisplay => FormatHelper.Bytes(FreeBytes);
    public double FreePercent => TotalBytes <= 0 ? 0 : FreeBytes * 100d / TotalBytes;
    public string HealthStatus { get; set; } = "No analizado";
    public string ScanStatus { get; set; } = "No analizado";
}

public sealed class DiagnosticIssue
{
    public DiagnosticSeverity Severity { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public string SuggestedAction { get; set; } = string.Empty;
    public RiskLevel Risk { get; set; } = RiskLevel.Low;
    public bool RequiresAdmin { get; set; }
    public bool IsReversible { get; set; }
}

public sealed class DiagnosticSnapshot
{
    public SystemSummary Summary { get; set; } = new();
    public DetectionSnapshot Processes { get; set; } = new();
    public List<ProcessInfoItem> AllProcesses { get; set; } = [];
    public List<StorageInfoItem> Storage { get; set; } = [];
    public List<ManualGamePath> ManualPaths { get; set; } = [];
    public List<DiagnosticIssue> Issues { get; set; } = [];
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.Now;
    public double RamUsagePercent => Summary.TotalRamBytes <= 0 ? 0 : Summary.UsedRamBytes * 100d / Summary.TotalRamBytes;
}

public sealed class DiagnosticCard
{
    public string Key { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string State { get; set; } = "No analizado";
    public string Explanation { get; set; } = string.Empty;
    public string Problems { get; set; } = "No detectado";
    public string SuggestedActions { get; set; } = string.Empty;
    public string RiskLabel { get; set; } = "Bajo";
    public bool RequiresAdmin { get; set; }
    public bool IsReversible { get; set; }
    public bool CanApply { get; set; }
    public string DisabledReason { get; set; } = string.Empty;
}

public sealed class ProfileStep
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public RiskLevel RiskLevel { get; set; }
    public bool RequiresAdmin { get; set; }
    public bool IsOptional { get; set; }
    public bool IsSelected { get; set; } = true;
}

public sealed class RecommendationItem
{
    public RecommendationLevel Level { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string TargetSection { get; set; } = string.Empty;
    public string SuggestedAction { get; set; } = string.Empty;
    public bool HasAction => !string.IsNullOrWhiteSpace(SuggestedAction);
    public string LevelLabel => Level switch
    {
        RecommendationLevel.Warning => "Advertencia",
        RecommendationLevel.SuggestedAction => "Acción sugerida",
        _ => "Info"
    };
}

public sealed class GuidedActionCard
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IncludedActions { get; set; } = string.Empty;
    public string RiskLabel { get; set; } = "Bajo";
    public string TargetSection { get; set; } = string.Empty;
    public bool IsImplemented { get; set; } = true;
    public string DisabledReason { get; set; } = string.Empty;
    public string PreviewText { get; set; } = string.Empty;
    public string ButtonText => IsImplemented ? "Abrir" : "Pendiente";
}

public sealed class ChecklistItem
{
    public string Name { get; set; } = string.Empty;
    public ChecklistState State { get; set; } = ChecklistState.Unknown;
    public string Detail { get; set; } = string.Empty;
    public string StateLabel => State switch
    {
        ChecklistState.Ok => "OK",
        ChecklistState.Warning => "Revisar",
        ChecklistState.Blocked => "Bloqueado",
        _ => "No verificado"
    };
}

public sealed class DetectionSnapshot
{
    public List<ProcessInfoItem> Games { get; set; } = [];
    public List<ProcessInfoItem> Launchers { get; set; } = [];
    public List<ProcessInfoItem> StreamingApps { get; set; } = [];
    public List<ProcessInfoItem> VoiceApps { get; set; } = [];
    public List<ProcessInfoItem> Browsers { get; set; } = [];
    public List<ProcessInfoItem> Overlays { get; set; } = [];
    public List<ProcessInfoItem> HeavyProcesses { get; set; } = [];
    public List<ProcessInfoItem> WallpaperApps { get; set; } = [];
    public List<ProcessInfoItem> TopProcessesByRam { get; set; } = [];
    public string GameStatus => Games.Count == 0 ? "No se detecto juego abierto" : string.Join(", ", Games.Take(3).Select(g => g.DisplayName));
    public string StreamingStatus => StreamingApps.Count == 0 ? "No se detecto app de streaming" : string.Join(", ", StreamingApps.Take(3).Select(g => g.DisplayName));
    public string VoiceStatus => VoiceApps.Count == 0 ? "No se detecto app de voz" : string.Join(", ", VoiceApps.Take(3).Select(g => g.DisplayName));
    public string WallpaperStatus => WallpaperApps.Count == 0 ? "Wallpaper Engine no detectado" : "Wallpaper Engine detectado";
    public string BrowserStatus => Browsers.Count == 0 ? "Sin navegadores destacados" : string.Join(", ", Browsers.Take(3).Select(g => g.DisplayName));
}
