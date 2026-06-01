using System.Runtime.InteropServices;
using NokaraSystemManager.Helpers;
using NokaraSystemManager.Models;

namespace NokaraSystemManager.Services;

public sealed class CleanerService
{
    private const uint SherbNoConfirmation = 0x00000001;
    private const uint SherbNoProgressUi = 0x00000002;
    private const uint SherbNoSound = 0x00000004;
    private readonly LoggingService _logging;

    public CleanerService(LoggingService logging)
    {
        _logging = logging;
    }

    public Task<IReadOnlyList<CleanupEstimateItem>> EstimateCleanableSpaceAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<CleanupEstimateItem> items =
        [
            Estimate("Temporales del usuario", Path.GetTempPath(), false),
            Estimate("Temporales de Windows", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp"), true),
            Estimate("Caché de miniaturas", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Windows", "Explorer"), false, "thumbcache_*.db"),
            Estimate("DirectX Shader Cache", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "D3DSCache"), false),
            Estimate("Logs viejos de Nokara", AppPaths.Logs, false, "*.log")
        ];
        return Task.FromResult(items);
    }

    public async Task<ActionResult> CleanUserTempAsync(CancellationToken cancellationToken = default) => await CleanPathAsync("Limpiar TEMP usuario", Path.GetTempPath(), "Temporales del usuario eliminados.", cancellationToken);
    public async Task<ActionResult> CleanWindowsTempAsync(CancellationToken cancellationToken = default) => await CleanPathAsync("Limpiar TEMP Windows", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp"), "Temporales de Windows eliminados.", cancellationToken);
    public async Task<ActionResult> CleanThumbnailCacheAsync(CancellationToken cancellationToken = default) => await CleanPathAsync("Limpiar miniaturas", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Windows", "Explorer"), "Caché de miniaturas limpiada.", cancellationToken, "thumbcache_*.db");
    public async Task<ActionResult> CleanDirectXShaderCacheAsync(CancellationToken cancellationToken = default) => await CleanPathAsync("Limpiar DirectX Shader Cache", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "D3DSCache"), "Cache DirectX Shader limpiada.", cancellationToken);

    public Task<ActionResult> CleanWindowsUpdateCacheAsync(CancellationToken cancellationToken = default)
    {
        var result = ActionResult.Fail("Limpiar caché Windows Update", "Acción deshabilitada en V1. Pendiente de implementación segura.");
        result.Details = "No se desactiva Windows Update ni se borran componentes críticos.";
        return Task.FromResult(result);
    }

    public async Task<ActionResult> CleanOldTemporaryLogsAsync(CancellationToken cancellationToken = default)
    {
        return await CleanPathAsync("Limpiar logs temporales", AppPaths.Logs, "Logs viejos de Nokara eliminados.", cancellationToken, "*.log", onlyOlderThanDays: 30);
    }

    public async Task<ActionResult> EmptyRecycleBinAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var code = SHEmptyRecycleBin(IntPtr.Zero, null, SherbNoConfirmation | SherbNoProgressUi | SherbNoSound);
            return code == 0 ? ActionResult.Ok("Vaciar papelera", "Papelera vaciada correctamente.") : ActionResult.Fail("Vaciar papelera", $"Windows devolvio codigo 0x{code:X}.");
        }
        catch (Exception ex)
        {
            await _logging.LogErrorAsync("Recycle bin cleanup failed.", ex);
            return ActionResult.Fail("Vaciar papelera", "No se pudo vaciar la papelera.", ex);
        }
    }

    private async Task<ActionResult> CleanPathAsync(string title, string path, string successMessage, CancellationToken cancellationToken, string pattern = "*", int? onlyOlderThanDays = null)
    {
        try
        {
            if (!Directory.Exists(path)) return ActionResult.Ok(title, "La carpeta no existe o no está disponible.");
            var freed = await Task.Run(() => CleanDirectory(path, pattern, cancellationToken, onlyOlderThanDays), cancellationToken);
            await _logging.LogInfoAsync($"{title}: {FormatHelper.Bytes(freed)}");
            return ActionResult.Ok(title, $"{successMessage} Espacio liberado: {FormatHelper.Bytes(freed)}", spaceFreedBytes: freed);
        }
        catch (Exception ex)
        {
            await _logging.LogErrorAsync($"{title} failed.", ex);
            return ActionResult.Fail(title, "No se pudo completar la limpieza. Algunos archivos pueden estar en uso.", ex);
        }
    }

    private static long CleanDirectory(string root, string pattern, CancellationToken token, int? onlyOlderThanDays)
    {
        var freed = 0L;
        foreach (var file in Directory.EnumerateFiles(root, pattern, SearchOption.AllDirectories))
        {
            token.ThrowIfCancellationRequested();
            try
            {
                var info = new FileInfo(file);
                if ((info.Attributes & FileAttributes.ReparsePoint) != 0) continue;
                if (onlyOlderThanDays.HasValue && info.LastWriteTimeUtc > DateTime.UtcNow.AddDays(-onlyOlderThanDays.Value)) continue;
                var length = info.Length;
                info.Attributes = FileAttributes.Normal;
                info.Delete();
                freed += length;
            }
            catch
            {
                // Ignore files in use or access denied.
            }
        }
        return freed;
    }

    private static CleanupEstimateItem Estimate(string name, string path, bool requiresAdmin, string pattern = "*")
    {
        var bytes = 0L;
        try
        {
            if (Directory.Exists(path))
            {
                bytes = Directory.EnumerateFiles(path, pattern, SearchOption.AllDirectories).Sum(f => { try { return new FileInfo(f).Length; } catch { return 0; } });
            }
        }
        catch { }
        return new CleanupEstimateItem { Name = name, Path = path, Bytes = bytes, SizeDisplay = FormatHelper.Bytes(bytes), RequiresAdmin = requiresAdmin };
    }

    [DllImport("Shell32.dll", CharSet = CharSet.Unicode)]
    private static extern uint SHEmptyRecycleBin(IntPtr hwnd, string? pszRootPath, uint dwFlags);
}
