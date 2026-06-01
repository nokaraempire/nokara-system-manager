using NokaraSystemManager.Models;

namespace NokaraSystemManager.Services;

public sealed class RestorePointService
{
    private readonly CommandRunnerService _commands;

    public RestorePointService(CommandRunnerService commands)
    {
        _commands = commands;
    }

    public async Task<ActionResult> CreateRestorePointAsync(CancellationToken cancellationToken = default)
    {
        const string script = "Checkpoint-Computer -Description 'Nokara System Manager' -RestorePointType 'MODIFY_SETTINGS'";
        var result = await _commands.RunAsync("powershell.exe", $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"", 120000, cancellationToken);
        return result.Success
            ? ActionResult.Ok("Punto de restauración", "Punto de restauración solicitado correctamente.", result.CombinedOutput)
            : ActionResult.Fail("Punto de restauración", "No se pudo crear el punto. Puede requerir administrador o Protección del sistema activa.", details: result.CombinedOutput);
    }
}
