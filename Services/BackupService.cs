using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using NokaraSystemManager.Helpers;
using NokaraSystemManager.Models;

namespace NokaraSystemManager.Services;

public sealed class BackupService
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true, Converters = { new JsonStringEnumConverter() } };

    public async Task AddRegistryBackupAsync(RegistryBackupItem item) => await UpdateListAsync<RegistryBackupItem>(AppPaths.RegistryBackupsFile, list => list.Add(item));
    public async Task<IReadOnlyList<RegistryBackupItem>> GetRegistryBackupsAsync() => await ReadListAsync<RegistryBackupItem>(AppPaths.RegistryBackupsFile);
    public async Task MarkRegistryRestoredAsync(string id, string result) => await UpdateListAsync<RegistryBackupItem>(AppPaths.RegistryBackupsFile, list => { var item = list.FirstOrDefault(i => i.Id == id); if (item is not null) { item.IsRestored = true; item.Result = result; } });
    public async Task AddDnsBackupAsync(DnsBackupItem item) => await UpdateListAsync<DnsBackupItem>(AppPaths.DnsBackupsFile, list => list.Add(item));
    public async Task<IReadOnlyList<DnsBackupItem>> GetDnsBackupsAsync() => await ReadListAsync<DnsBackupItem>(AppPaths.DnsBackupsFile);
    public async Task MarkDnsRestoredAsync(string id) => await UpdateListAsync<DnsBackupItem>(AppPaths.DnsBackupsFile, list => { var item = list.FirstOrDefault(i => i.Id == id); if (item is not null) item.IsRestored = true; });
    public async Task AddPowerPlanBackupAsync(PowerPlanBackupItem item) => await UpdateListAsync<PowerPlanBackupItem>(AppPaths.PowerPlanBackupsFile, list => list.Add(item));
    public async Task<IReadOnlyList<PowerPlanBackupItem>> GetPowerPlanBackupsAsync() => await ReadListAsync<PowerPlanBackupItem>(AppPaths.PowerPlanBackupsFile);
    public async Task MarkPowerPlanRestoredAsync(string id) => await UpdateListAsync<PowerPlanBackupItem>(AppPaths.PowerPlanBackupsFile, list => { var item = list.FirstOrDefault(i => i.Id == id); if (item is not null) item.IsRestored = true; });

    public async Task AddActionHistoryAsync(string module, string actionId, string actionName, ActionResult result)
    {
        var item = new ActionHistoryItem
        {
            Module = module,
            ActionId = actionId,
            ActionName = actionName,
            Success = result.Success,
            Message = result.Message,
            Details = result.Details,
            Error = result.Error,
            SpaceFreedBytes = result.SpaceFreedBytes,
            RequiresRestart = result.RequiresRestart,
            RequiresAdmin = result.RequiresAdmin,
            WasCancelled = result.WasCancelled
        };
        await UpdateListAsync<ActionHistoryItem>(AppPaths.ActionHistoryFile, list => list.Add(item));
    }

    public async Task<IReadOnlyList<ActionHistoryItem>> GetActionHistoryAsync() => await ReadListAsync<ActionHistoryItem>(AppPaths.ActionHistoryFile);

    public async Task<int> GetReversibleChangeCountAsync()
    {
        var registry = await GetRegistryBackupsAsync();
        var dns = await GetDnsBackupsAsync();
        var plans = await GetPowerPlanBackupsAsync();
        return registry.Count(i => i.CanRestore && !i.IsRestored) + dns.Count(i => i.CanRestore && !i.IsRestored) + plans.Count(i => i.CanRestore && !i.IsRestored);
    }

    public async Task<string> ExportReportAsync(SystemSummary summary, IEnumerable<ProcessInfoItem>? processes = null)
    {
        AppPaths.EnsureBaseFolders();
        var history = await GetActionHistoryAsync();
        var registry = await GetRegistryBackupsAsync();
        var dns = await GetDnsBackupsAsync();
        var plans = await GetPowerPlanBackupsAsync();
        var reportPath = Path.Combine(AppPaths.Logs, $"nokara-report-{DateTime.Now:yyyyMMdd-HHmmss}.txt");
        var builder = new StringBuilder();
        builder.AppendLine("Nokara System Manager - Reporte");
        builder.AppendLine($"Fecha: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        builder.AppendLine();
        builder.AppendLine("Sistema");
        builder.AppendLine($"Equipo: {summary.MachineName}");
        builder.AppendLine($"Usuario: {summary.UserName}");
        builder.AppendLine($"Windows: {summary.WindowsVersion}");
        builder.AppendLine($"Admin: {(summary.IsAdmin ? "Sí" : "No")}");
        builder.AppendLine($"CPU: {summary.CpuName}");
        builder.AppendLine($"RAM: {summary.UsedRamDisplay} / {summary.TotalRamDisplay}");
        builder.AppendLine($"GPU: {summary.GpuDisplay}");
        builder.AppendLine($"DNS actual: {summary.CurrentDns}");
        builder.AppendLine($"Plan energía: {summary.CurrentPowerPlan}");
        builder.AppendLine();
        builder.AppendLine("Acciones recientes");
        foreach (var item in history.OrderByDescending(i => i.Timestamp).Take(75))
        {
            builder.AppendLine($"- {item.Timestamp:yyyy-MM-dd HH:mm:ss} [{item.Module}] {item.ActionName}: {(item.Success ? "OK" : item.WasCancelled ? "CANCEL" : "ERROR")} - {item.Message}");
        }

        builder.AppendLine();
        builder.AppendLine("Procesos principales");
        foreach (var process in (processes ?? []).OrderByDescending(p => p.MemoryMB).Take(25))
        {
            builder.AppendLine($"- {process.ProcessName} PID {process.Pid} [{process.Category}] RAM {process.MemoryMB} MB Prioridad {process.Priority}");
        }

        builder.AppendLine();
        builder.AppendLine("Backups reversibles");
        builder.AppendLine($"Registro: {registry.Count(i => i.CanRestore && !i.IsRestored)}");
        builder.AppendLine($"DNS: {dns.Count(i => i.CanRestore && !i.IsRestored)}");
        builder.AppendLine($"Plan energía: {plans.Count(i => i.CanRestore && !i.IsRestored)}");
        await File.WriteAllTextAsync(reportPath, builder.ToString());
        return reportPath;
    }

    private async Task<IReadOnlyList<T>> ReadListAsync<T>(string path)
    {
        await _semaphore.WaitAsync();
        try
        {
            AppPaths.EnsureBaseFolders();
            if (!File.Exists(path)) return [];
            var json = await File.ReadAllTextAsync(path);
            return string.IsNullOrWhiteSpace(json) ? [] : JsonSerializer.Deserialize<List<T>>(json, _jsonOptions) ?? [];
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task UpdateListAsync<T>(string path, Action<List<T>> update)
    {
        await _semaphore.WaitAsync();
        try
        {
            AppPaths.EnsureBaseFolders();
            var list = File.Exists(path)
                ? JsonSerializer.Deserialize<List<T>>(await File.ReadAllTextAsync(path), _jsonOptions) ?? []
                : [];
            update(list);
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(list, _jsonOptions));
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
