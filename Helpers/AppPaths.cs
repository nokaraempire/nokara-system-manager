namespace NokaraSystemManager.Helpers;

public static class AppPaths
{
    public static string AppDataRoot { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "NokaraSystemManager");

    public static string Logs { get; } = Path.Combine(AppDataRoot, "Logs");
    public static string Backups { get; } = Path.Combine(AppDataRoot, "Backups");
    public static string Config { get; } = Path.Combine(AppDataRoot, "Config");
    public static string Reports { get; } = Path.Combine(AppDataRoot, "Reports");
    public static string LiteLogs { get; } = Path.Combine(AppDataRoot, "LiteLogs");
    public static string LauncherLogs { get; } = Path.Combine(AppDataRoot, "LauncherLogs");
    public static string ActionHistoryFile => Path.Combine(Backups, "action-history.json");
    public static string RegistryBackupsFile => Path.Combine(Backups, "registry-backups.json");
    public static string DnsBackupsFile => Path.Combine(Backups, "dns-backups.json");
    public static string PowerPlanBackupsFile => Path.Combine(Backups, "powerplan-backups.json");
    public static string SettingsFile => Path.Combine(Config, "settings.json");
    public static string KnownProcessesFile => Path.Combine(Config, "known-processes.json");
    public static string SocialLinksFile => Path.Combine(Config, "social-links.json");
    public static string ManualPathsFile => Path.Combine(Config, "manual-paths.json");

    public static void EnsureBaseFolders()
    {
        Directory.CreateDirectory(AppDataRoot);
        Directory.CreateDirectory(Logs);
        Directory.CreateDirectory(Backups);
        Directory.CreateDirectory(Config);
        Directory.CreateDirectory(Reports);
        Directory.CreateDirectory(LiteLogs);
        Directory.CreateDirectory(LauncherLogs);
    }
}
