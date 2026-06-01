using System.Text.RegularExpressions;
using NokaraSystemManager.Models;

namespace NokaraSystemManager.Services;

public sealed class PowerPlanService
{
    private readonly CommandRunnerService _commands;
    private readonly BackupService _backup;

    public PowerPlanService(CommandRunnerService commands, BackupService backup)
    {
        _commands = commands;
        _backup = backup;
    }

    public async Task<string> GetCurrentPlanDisplayAsync()
    {
        var result = await _commands.RunAsync("powercfg.exe", "/getactivescheme", 30000);
        return result.Success ? result.CombinedOutput.Trim() : "No detectado";
    }

    public async Task<ActionResult> ActivateBestPerformancePlanAsync(CancellationToken cancellationToken = default)
    {
        var current = await _commands.RunAsync("powercfg.exe", "/getactivescheme", 30000, cancellationToken);
        var previousGuid = ExtractGuid(current.CombinedOutput);
        var previousName = current.CombinedOutput.Trim();
        var high = "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c";
        await _backup.AddPowerPlanBackupAsync(new PowerPlanBackupItem { PreviousPlanGuid = previousGuid, PreviousPlanName = previousName, NewPlanGuid = high, NewPlanName = "Alto rendimiento" });
        var activate = await _commands.RunAsync("powercfg.exe", $"/setactive {high}", 30000, cancellationToken);
        return activate.Success ? ActionResult.Ok("Plan de energía", "Plan Alto rendimiento activado.", activate.CombinedOutput) : ActionResult.Fail("Plan de energía", "No se pudo activar Alto rendimiento.", details: activate.CombinedOutput);
    }

    public async Task<ActionResult> RestoreLatestAsync(CancellationToken cancellationToken = default)
    {
        var backup = (await _backup.GetPowerPlanBackupsAsync()).OrderByDescending(b => b.Timestamp).FirstOrDefault(b => b.CanRestore && !b.IsRestored);
        if (backup is null) return ActionResult.Ok("Restaurar energía", "No hay backup de plan de energía pendiente.");
        var result = await _commands.RunAsync("powercfg.exe", $"/setactive {backup.PreviousPlanGuid}", 30000, cancellationToken);
        if (result.Success)
        {
            await _backup.MarkPowerPlanRestoredAsync(backup.Id);
            return ActionResult.Ok("Restaurar energía", $"Plan restaurado: {backup.PreviousPlanName}", result.CombinedOutput);
        }
        return ActionResult.Fail("Restaurar energía", "No se pudo restaurar el plan de energía.", details: result.CombinedOutput);
    }

    private static string ExtractGuid(string text)
    {
        return Regex.Match(text, "[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}").Value;
    }
}
