using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using NokaraSystemManager.Helpers;
using NokaraSystemManager.Models;
using NokaraSystemManager.Services;

namespace NokaraSystemManager.ViewModels;

public sealed class MainViewModel : ViewModelBase
{
    private readonly AppServices _services;
    private NavigationItemViewModel? _selectedNavigationItem;
    private ViewModelBase? _currentViewModel;
    private string _statusMessage = "Modo avanzado activo. Las acciones sensibles requieren confirmacion.";
    private bool _isAdmin;
    private bool _isBusy;
    private bool _showPendingNotice = true;

    public MainViewModel(AppServices services)
    {
        _services = services;
        Dashboard = new DashboardViewModel(services, SelectSection);
        Maintenance = new MaintenanceViewModel(services, CreateActionCard);
        Diagnostic = new DiagnosticViewModel(services);
        Gaming = new GamingViewModel(services, CreateActionCard);
        Network = new NetworkViewModel(services, OnActionCompletedAsync);
        Streaming = new StreamingViewModel(services, CreateActionCard);
        Processes = new ProcessesViewModel(services, OnActionCompletedAsync);
        Privacy = new PrivacyViewModel(services, CreateActionCard);
        Profiles = new ProfilesViewModel(services, OnActionCompletedAsync);
        Backups = new BackupRestoreViewModel(services, OnActionCompletedAsync);
        Settings = new SettingsViewModel(services);
        About = new AboutViewModel(services);

        NavigationItems =
        [
            new NavigationItemViewModel("Dashboard", "IN", "\uE80F", "dashboard", Dashboard),
            new NavigationItemViewModel("Mantenimiento", "MT", "\uE90F", "mantenimiento", Maintenance),
            new NavigationItemViewModel("Diagnostico", "DG", "\uE9D9", "diagnostico", Diagnostic),
            new NavigationItemViewModel("Gaming", "GM", "\uE7FC", "gaming", Gaming),
            new NavigationItemViewModel("Red", "RD", "\uE968", "red", Network),
            new NavigationItemViewModel("Streaming", "ST", "\uE714", "streaming", Streaming),
            new NavigationItemViewModel("Procesos", "PR", "\uE9D2", "procesos", Processes),
            new NavigationItemViewModel("Privacidad", "PV", "\uE72E", "privacidad", Privacy),
            new NavigationItemViewModel("Backups", "BK", "\uE74E", "backups", Backups),
            new NavigationItemViewModel("Ajustes", "AJ", "\uE713", "ajustes", Settings)
        ];

        InitializeCommand = new AsyncRelayCommand(InitializeAsync);
        RefreshCommand = new AsyncRelayCommand(RefreshCurrentAsync);
        ExportReportCommand = new AsyncRelayCommand(ExportReportAsync);
        ShowAboutCommand = new AsyncRelayCommand(ShowAboutAsync);
        RestartAsAdminCommand = new RelayCommand(RestartAsAdmin);
        DismissPendingNoticeCommand = new RelayCommand(() => ShowPendingNotice = false);

        SelectedNavigationItem = NavigationItems[0];
    }

    public ObservableCollection<NavigationItemViewModel> NavigationItems { get; }
    public DashboardViewModel Dashboard { get; }
    public MaintenanceViewModel Maintenance { get; }
    public DiagnosticViewModel Diagnostic { get; }
    public GamingViewModel Gaming { get; }
    public NetworkViewModel Network { get; }
    public StreamingViewModel Streaming { get; }
    public ProcessesViewModel Processes { get; }
    public PrivacyViewModel Privacy { get; }
    public ProfilesViewModel Profiles { get; }
    public BackupRestoreViewModel Backups { get; }
    public SettingsViewModel Settings { get; }
    public AboutViewModel About { get; }
    public ICommand InitializeCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand ExportReportCommand { get; }
    public ICommand ShowAboutCommand { get; }
    public ICommand RestartAsAdminCommand { get; }
    public ICommand DismissPendingNoticeCommand { get; }

    public NavigationItemViewModel? SelectedNavigationItem
    {
        get => _selectedNavigationItem;
        set
        {
            if (ReferenceEquals(_selectedNavigationItem, value))
            {
                return;
            }

            if (SetProperty(ref _selectedNavigationItem, value))
            {
                if (value is not null)
                {
                    CurrentViewModel = value.ViewModel;
                    StatusMessage = $"Seccion activa: {value.Title}.";
                    _ = RefreshCurrentAsync();
                }

                OnPropertyChanged(nameof(CurrentSectionTitle));
            }
        }
    }

    public ViewModelBase? CurrentViewModel
    {
        get => _currentViewModel;
        private set => SetProperty(ref _currentViewModel, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public bool IsAdmin
    {
        get => _isAdmin;
        private set
        {
            if (SetProperty(ref _isAdmin, value))
            {
                OnPropertyChanged(nameof(AdminDisplay));
            }
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    public bool ShowPendingNotice
    {
        get => _showPendingNotice;
        private set => SetProperty(ref _showPendingNotice, value);
    }

    public string AdminDisplay => IsAdmin ? "Administrador: Si" : "Administrador: No";
    public string ModeDisplay => "Modo avanzado";
    public string CurrentSectionTitle => SelectedNavigationItem?.Title ?? "Acerca de";

    public async Task InitializeAsync()
    {
        IsBusy = true;
        try
        {
            await _services.Settings.EnsureKnownProcessesAsync();
            var settings = await _services.Settings.LoadAsync();
            if (!settings.ExpertMode)
            {
                settings.ExpertMode = true;
                await _services.Settings.SaveAsync(settings);
            }

            IsAdmin = _services.Admin.IsRunningAsAdmin();
            await Dashboard.LoadAsync();
            await Maintenance.LoadAsync();
            await About.LoadAsync();
            StatusMessage = "Modo avanzado activo. Las acciones sensibles siguen pidiendo confirmacion.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private ActionCardViewModel CreateActionCard(OptimizationAction action)
    {
        return new ActionCardViewModel(action, _services, OnActionCompletedAsync);
    }

    private async Task OnActionCompletedAsync(OptimizationAction action, ActionResult result)
    {
        if (result.Success)
        {
            var settings = await _services.Settings.LoadAsync();
            if (string.Equals(action.Module, "Mantenimiento", StringComparison.OrdinalIgnoreCase))
            {
                settings.LastCleanup = DateTimeOffset.Now;
            }
            else if (string.Equals(action.Module, "Gaming", StringComparison.OrdinalIgnoreCase))
            {
                settings.LastGamingOptimization = DateTimeOffset.Now;
            }

            await _services.Settings.SaveAsync(settings);
        }

        StatusMessage = $"{action.Name}: {result.Message}";
        await Dashboard.LoadAsync();
    }

    private async Task RefreshCurrentAsync()
    {
        switch (CurrentViewModel)
        {
            case DashboardViewModel vm: await vm.LoadAsync(); break;
            case MaintenanceViewModel vm: await vm.LoadAsync(); break;
            case DiagnosticViewModel vm: await vm.LoadAsync(); break;
            case GamingViewModel vm: await vm.LoadAsync(); break;
            case NetworkViewModel vm: await vm.LoadAsync(); break;
            case StreamingViewModel vm: await vm.LoadAsync(); break;
            case ProcessesViewModel vm: await vm.LoadAsync(); break;
            case PrivacyViewModel: break;
            case BackupRestoreViewModel vm: await vm.LoadAsync(); break;
            case SettingsViewModel vm: await vm.LoadAsync(); break;
            case AboutViewModel vm: await vm.LoadAsync(); break;
        }
    }

    private async Task ExportReportAsync()
    {
        var path = await _services.Reports.GenerateSystemReportAsync("Reporte manual", openNotepad: true);
        StatusMessage = $"Reporte exportado: {path}";
    }

    private async Task ShowAboutAsync()
    {
        SelectedNavigationItem = null;
        CurrentViewModel = About;
        await About.LoadAsync();
        StatusMessage = "Acerca de Nokara System Manager.";
        OnPropertyChanged(nameof(CurrentSectionTitle));
    }

    private void RestartAsAdmin()
    {
        if (_services.Admin.RestartAsAdmin())
        {
            Application.Current.Shutdown();
        }
    }

    private void SelectSection(string section)
    {
        if (string.IsNullOrWhiteSpace(section))
        {
            return;
        }

        if (string.Equals(section, "Acerca de", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(section, "About", StringComparison.OrdinalIgnoreCase))
        {
            _ = ShowAboutAsync();
            return;
        }

        var lookup = section.Trim();
        if (lookup.StartsWith("diagn", StringComparison.OrdinalIgnoreCase))
        {
            lookup = "Diagnostico";
        }

        var item = NavigationItems.FirstOrDefault(n =>
            string.Equals(n.Title, lookup, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(n.RouteSegment, lookup, StringComparison.OrdinalIgnoreCase));
        if (item is not null)
        {
            SelectedNavigationItem = item;
        }
    }
}
