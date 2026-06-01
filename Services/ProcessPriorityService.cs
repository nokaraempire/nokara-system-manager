using System.Diagnostics;
using NokaraSystemManager.Models;

namespace NokaraSystemManager.Services;

public sealed class ProcessPriorityService
{
    private readonly LoggingService _logging;

    public ProcessPriorityService(LoggingService logging)
    {
        _logging = logging;
    }

    public async Task<ActionResult> SetPriorityAsync(ProcessInfoItem item, ProcessPriorityClass priority)
    {
        if (item.IsCritical || !item.CanChangePriority)
        {
            return ActionResult.Fail("Cambiar prioridad", "La app bloquea cambios sobre procesos críticos de Windows.");
        }

        if (priority == ProcessPriorityClass.RealTime)
        {
            return ActionResult.Fail("Cambiar prioridad", "Prioridad Tiempo real no está permitida por seguridad.");
        }

        try
        {
            using var process = Process.GetProcessById(item.Pid);
            process.PriorityClass = priority;
            await _logging.LogInfoAsync($"Priority changed: {item.ProcessName} ({item.Pid}) -> {priority}");
            return ActionResult.Ok("Cambiar prioridad", $"{item.ProcessName} ahora usa prioridad {priority}.");
        }
        catch (Exception ex)
        {
            await _logging.LogErrorAsync($"Priority change failed: {item.ProcessName} ({item.Pid})", ex);
            return ActionResult.Fail("Cambiar prioridad", "No se pudo cambiar la prioridad. Puede requerir permisos o el proceso pudo cerrarse.", ex);
        }
    }
}
