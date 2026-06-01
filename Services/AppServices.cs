namespace NokaraSystemManager.Services;

public sealed class AppServices
{
    public AppServices()
    {
        Logging = new LoggingService();
        Backup = new BackupService();
        Settings = new SettingsService();
        Admin = new AdminService();
        Commands = new CommandRunnerService(Logging);
        Registry = new RegistryService(Backup, Logging);
        Cleaner = new CleanerService(Logging);
        PowerPlan = new PowerPlanService(Commands, Backup);
        Gaming = new GamingOptimizationService(Registry, PowerPlan);
        Privacy = new PrivacyOptimizationService(Registry);
        Network = new NetworkService(Commands, Backup, Logging);
        DnsBenchmark = new DnsBenchmarkService();
        Processes = new ProcessDetectionService(Settings, Logging);
        ProcessPriority = new ProcessPriorityService(Logging);
        Profiles = new ProfileService();
        RestorePoints = new RestorePointService(Commands);
        SystemInfo = new SystemInfoService(Admin, Backup, Settings, Network, PowerPlan);
        SocialLinks = new SocialLinksService();
        ManualPaths = new ManualPathsService();
        Diagnostics = new DiagnosticService(SystemInfo, Processes, ManualPaths, Commands, Logging);
        Reports = new ReportService(Diagnostics, Backup, Logging);
    }

    public LoggingService Logging { get; }
    public BackupService Backup { get; }
    public SettingsService Settings { get; }
    public AdminService Admin { get; }
    public CommandRunnerService Commands { get; }
    public RegistryService Registry { get; }
    public CleanerService Cleaner { get; }
    public PowerPlanService PowerPlan { get; }
    public GamingOptimizationService Gaming { get; }
    public PrivacyOptimizationService Privacy { get; }
    public NetworkService Network { get; }
    public DnsBenchmarkService DnsBenchmark { get; }
    public ProcessDetectionService Processes { get; }
    public ProcessPriorityService ProcessPriority { get; }
    public ProfileService Profiles { get; }
    public RestorePointService RestorePoints { get; }
    public SystemInfoService SystemInfo { get; }
    public SocialLinksService SocialLinks { get; }
    public ManualPathsService ManualPaths { get; }
    public DiagnosticService Diagnostics { get; }
    public ReportService Reports { get; }
}
