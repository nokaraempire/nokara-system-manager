using System.Diagnostics;
using System.Security.Principal;

namespace NokaraSystemManager.Services;

public sealed class AdminService
{
    public bool IsRunningAsAdmin()
    {
        using var identity = WindowsIdentity.GetCurrent();
        return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
    }

    public bool RestartAsAdmin()
    {
        var exe = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(exe))
        {
            return false;
        }

        Process.Start(new ProcessStartInfo(exe) { UseShellExecute = true, Verb = "runas" });
        return true;
    }
}
