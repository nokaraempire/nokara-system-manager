using System.Runtime.InteropServices;
using System.Reflection;
using NokaraSystemManager.Models;

namespace NokaraSystemManager.Services;

public sealed class SystemInfoService
{
    private readonly AdminService _admin;
    private readonly BackupService _backup;
    private readonly SettingsService _settings;
    private readonly NetworkService _network;
    private readonly PowerPlanService _powerPlan;

    public SystemInfoService(AdminService admin, BackupService backup, SettingsService settings, NetworkService network, PowerPlanService powerPlan)
    {
        _admin = admin;
        _backup = backup;
        _settings = settings;
        _network = network;
        _powerPlan = powerPlan;
    }

    public async Task<SystemSummary> GetSummaryAsync()
    {
        var settings = await _settings.LoadAsync();
        var history = await _backup.GetActionHistoryAsync();
        var adapters = _network.GetAdapters();
        var activeAdapter = adapters.FirstOrDefault(a => a.IsActive) ?? adapters.FirstOrDefault();
        var systemDrive = DriveInfo.GetDrives().FirstOrDefault(d => d.IsReady && d.RootDirectory.FullName.Equals(Path.GetPathRoot(Environment.SystemDirectory), StringComparison.OrdinalIgnoreCase));
        var summary = new SystemSummary
        {
            WindowsVersion = RuntimeInformation.OSDescription.Trim(),
            Architecture = RuntimeInformation.OSArchitecture.ToString(),
            AppVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.1",
            IsAdmin = _admin.IsRunningAsAdmin(),
            CpuName = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? "No detectado",
            TotalRamBytes = GetTotalRam(),
            UsedRamBytes = GetUsedRam(),
            GpuNames = ["Detectar GPU no disponible sin WMI"],
            SystemDriveFreeBytes = systemDrive?.AvailableFreeSpace ?? 0,
            ActiveNetworkAdapter = activeAdapter?.Name ?? "Sin adaptador activo",
            IpAddress = activeAdapter?.IpAddress ?? "No detectada",
            Gateway = activeAdapter?.Gateway ?? "No detectado",
            CurrentDns = activeAdapter?.DnsDisplay ?? "No detectado",
            ConnectionStatus = activeAdapter?.IsActive == true ? "Conectado" : "Sin conexión activa detectada",
            CurrentPowerPlan = await _powerPlan.GetCurrentPlanDisplayAsync(),
            LastCleanup = settings.LastCleanup?.ToString("yyyy-MM-dd HH:mm") ?? "Nunca",
            LastGamingOptimization = settings.LastGamingOptimization?.ToString("yyyy-MM-dd HH:mm") ?? "Nunca",
            LastAction = history.OrderByDescending(h => h.Timestamp).FirstOrDefault()?.ActionName ?? "Ninguna",
            ReversibleChangesCount = await _backup.GetReversibleChangeCountAsync()
        };

        return summary;
    }

    private static long GetTotalRam()
    {
        return GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
    }

    private static long GetUsedRam()
    {
        try
        {
            var total = GetTotalRam();
            var available = GC.GetGCMemoryInfo().HighMemoryLoadThresholdBytes - GC.GetTotalMemory(false);
            return Math.Clamp(total - Math.Max(0, available), 0, total);
        }
        catch
        {
            return 0;
        }
    }
}
