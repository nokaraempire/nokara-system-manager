using System.Diagnostics;
using NokaraSystemManager.Models;

namespace NokaraSystemManager.Services;

public sealed class CommandRunnerService
{
    private readonly LoggingService _logging;

    public CommandRunnerService(LoggingService logging)
    {
        _logging = logging;
    }

    public async Task<CommandResult> RunAsync(string fileName, string arguments, int timeoutMs = 60000, CancellationToken cancellationToken = default)
    {
        var result = new CommandResult();
        var stopwatch = Stopwatch.StartNew();
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo(fileName, arguments)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            process.Start();
            var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
            var exited = await Task.Run(() => process.WaitForExit(timeoutMs), cancellationToken);
            if (!exited)
            {
                result.TimedOut = true;
                try { process.Kill(entireProcessTree: true); } catch { }
            }

            result.ExitCode = exited ? process.ExitCode : -1;
            result.StandardOutput = await outputTask;
            result.StandardError = await errorTask;
        }
        catch (Exception ex)
        {
            result.ExitCode = -1;
            result.StandardError = ex.ToString();
            await _logging.LogErrorAsync($"Command failed: {fileName} {arguments}", ex);
        }
        finally
        {
            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;
        }

        return result;
    }
}
