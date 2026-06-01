using System.Diagnostics;
using NokaraSystemManager.Models;

namespace NokaraSystemManager.Services;

public sealed class ProcessDetectionService
{
    private static readonly HashSet<string> CriticalProcesses = new(StringComparer.OrdinalIgnoreCase)
    {
        "csrss", "dwm", "explorer", "lsass", "services", "smss", "spoolsv", "svchost", "system", "wininit", "winlogon"
    };

    private static readonly HashSet<string> GameNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "valorant", "cs2", "csgo", "fortniteclient-win64-shipping", "r5apex", "cod", "cod22-cod", "league of legends", "leagueclient"
    };

    private static readonly HashSet<string> OverlayNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "gamebar", "gamebarftserver", "xboxgamebar", "xboxapp", "nvidia share", "nvidiacontainer", "nvcontainer", "radeonsoftware", "steamwebhelper", "discord"
    };

    private static readonly HashSet<string> StreamingNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "obs64", "obs32", "streamlabs obs", "streamlabs", "tiktok live studio", "tiktoklive studio"
    };

    private static readonly HashSet<string> VoiceNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "discord", "teamspeak", "ts3client", "mumble"
    };

    private static readonly HashSet<string> BrowserNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "chrome", "msedge", "firefox", "brave", "opera", "operagx", "vivaldi"
    };

    private static readonly HashSet<string> WallpaperNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "wallpaper64", "wallpaper32", "wallpaperengine"
    };

    private readonly SettingsService _settings;
    private readonly LoggingService _logging;

    public ProcessDetectionService(SettingsService settings, LoggingService logging)
    {
        _settings = settings;
        _logging = logging;
    }

    public async Task<IReadOnlyList<ProcessInfoItem>> GetProcessesAsync()
    {
        var known = (await _settings.LoadKnownProcessesAsync()).ToDictionary(k => k.ProcessName, StringComparer.OrdinalIgnoreCase);
        var items = new List<ProcessInfoItem>();

        foreach (var process in Process.GetProcesses())
        {
            try
            {
                var name = process.ProcessName;
                known.TryGetValue(name, out var definition);
                var critical = CriticalProcesses.Contains(name);
                var category = definition?.Category ?? InferCategory(name, critical);
                var memory = Math.Round(process.WorkingSet64 / 1024d / 1024d, 1);
                items.Add(new ProcessInfoItem
                {
                    ProcessName = name,
                    DisplayName = definition?.DisplayName ?? name,
                    Pid = process.Id,
                    Category = category,
                    Priority = TryGetPriority(process),
                    MemoryMB = memory,
                    IsKnown = definition is not null,
                    IsCritical = critical,
                    CanChangePriority = !critical,
                    Notes = definition?.Notes ?? BuildNotes(name, category, critical),
                    RecommendedAction = BuildRecommendedAction(name, category, memory, critical),
                    IsHighMemory = memory >= 500
                });
            }
            catch (Exception ex)
            {
                await _logging.LogErrorAsync("Process scan item failed.", ex);
            }
            finally
            {
                process.Dispose();
            }
        }

        return items.OrderByDescending(p => p.MemoryMB).ThenBy(p => p.ProcessName).ToList();
    }

    public async Task<DetectionSnapshot> GetSnapshotAsync()
    {
        var processes = (await GetProcessesAsync()).ToList();
        return new DetectionSnapshot
        {
            Games = processes.Where(p => p.Category == ProcessCategory.Game).Take(8).ToList(),
            Launchers = processes.Where(p => p.Category == ProcessCategory.GameLauncher).Take(8).ToList(),
            StreamingApps = processes.Where(p => p.Category == ProcessCategory.Streaming).Take(8).ToList(),
            VoiceApps = processes.Where(p => p.Category == ProcessCategory.Communication).Take(8).ToList(),
            Browsers = processes.Where(p => p.Category == ProcessCategory.Browser).Take(8).ToList(),
            Overlays = processes.Where(p => OverlayNames.Contains(p.ProcessName)).Take(8).ToList(),
            HeavyProcesses = processes.Where(p => !p.IsCritical && p.MemoryMB >= 500).Take(10).ToList(),
            WallpaperApps = processes.Where(p => WallpaperNames.Contains(p.ProcessName)).Take(4).ToList(),
            TopProcessesByRam = processes.Where(p => !p.IsCritical).Take(12).ToList()
        };
    }

    private static ProcessPriorityClass? TryGetPriority(Process process)
    {
        try
        {
            return process.PriorityClass;
        }
        catch
        {
            return null;
        }
    }

    private static ProcessCategory InferCategory(string processName, bool critical)
    {
        if (critical) return ProcessCategory.System;
        if (GameNames.Contains(processName)) return ProcessCategory.Game;
        if (WallpaperNames.Contains(processName)) return ProcessCategory.Multimedia;
        if (StreamingNames.Contains(processName)) return ProcessCategory.Streaming;
        if (VoiceNames.Contains(processName)) return ProcessCategory.Communication;
        if (BrowserNames.Contains(processName)) return ProcessCategory.Browser;
        if (processName.Contains("steam", StringComparison.OrdinalIgnoreCase) ||
            processName.Contains("epic", StringComparison.OrdinalIgnoreCase) ||
            processName.Contains("battle.net", StringComparison.OrdinalIgnoreCase) ||
            processName.Contains("riotclient", StringComparison.OrdinalIgnoreCase))
        {
            return ProcessCategory.GameLauncher;
        }

        return ProcessCategory.Unknown;
    }

    private static string BuildNotes(string processName, ProcessCategory category, bool critical)
    {
        if (critical) return "Proceso critico de Windows. Nokara no lo toca.";
        if (WallpaperNames.Contains(processName)) return "Puede afectar FPS en modo competitivo. Cerrar solo con confirmación.";
        return category switch
        {
            ProcessCategory.Game => "Juego detectado. Puede priorizarse manualmente.",
            ProcessCategory.GameLauncher => "Launcher. Puede quedar abierto tras iniciar un juego.",
            ProcessCategory.Streaming => "App de streaming/captura detectada.",
            ProcessCategory.Communication => "App de voz/chat detectada.",
            ProcessCategory.Browser => "Navegador. Revisar si consume RAM durante gaming/stream.",
            _ => "Proceso de usuario o servicio no clasificado."
        };
    }

    private static string BuildRecommendedAction(string processName, ProcessCategory category, double memory, bool critical)
    {
        if (critical) return "No modificar";
        if (WallpaperNames.Contains(processName)) return "Pausar o cerrar antes de competitivo";
        if (memory >= 1000 && category != ProcessCategory.Game && category != ProcessCategory.Streaming) return "Revisar consumo antes de jugar";
        return category switch
        {
            ProcessCategory.Game => "Priorizar si estas jugando",
            ProcessCategory.Streaming => "AboveNormal si estas transmitiendo",
            ProcessCategory.GameLauncher => "Cerrar tras iniciar el juego si no lo necesitas",
            ProcessCategory.Browser => "Cerrar pestanas pesadas si falta RAM",
            ProcessCategory.Communication => "Mantener Normal o AboveNormal",
            _ => "Revisar manualmente"
        };
    }
}
