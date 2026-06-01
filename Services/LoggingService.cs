using NokaraSystemManager.Helpers;
using NokaraSystemManager.Models;

namespace NokaraSystemManager.Services;

public sealed class LoggingService
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    public string CurrentLogFile => Path.Combine(AppPaths.Logs, $"nokara-system-manager-{DateTime.Now:yyyy-MM-dd}.log");

    public async Task LogInfoAsync(string message) => await WriteAsync("INFO", message);
    public async Task LogWarningAsync(string message) => await WriteAsync("WARN", message);
    public async Task LogErrorAsync(string message, Exception? exception = null) => await WriteAsync("ERROR", exception is null ? message : $"{message}{Environment.NewLine}{exception}");

    public async Task LogActionResultAsync(string module, string action, ActionResult result)
    {
        var status = result.Success ? "OK" : result.WasCancelled ? "CANCEL" : "ERROR";
        var flags = new List<string>();
        if (result.WasCancelled) flags.Add("cancelada por el usuario");
        if (result.RequiresAdmin) flags.Add("requiere admin");
        if (result.RequiresRestart) flags.Add("requiere reinicio");
        if (result.SpaceFreedBytes > 0) flags.Add($"espacio liberado: {FormatHelper.Bytes(result.SpaceFreedBytes)}");
        var suffix = flags.Count == 0 ? string.Empty : $" ({string.Join(", ", flags)})";
        var message = $"{module} - {action} - {status} - {result.Message}{suffix}";
        if (!string.IsNullOrWhiteSpace(result.Error))
        {
            message += $"{Environment.NewLine}{result.Error}";
        }

        await WriteAsync("ACTION", message);
    }

    private async Task WriteAsync(string level, string message)
    {
        AppPaths.EnsureBaseFolders();
        var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}{Environment.NewLine}";
        await _semaphore.WaitAsync();
        try
        {
            await File.AppendAllTextAsync(CurrentLogFile, line);
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
