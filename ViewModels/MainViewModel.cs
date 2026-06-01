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
    private readonly Stack<NavigationItemViewModel> _backHistory = new();
    private readonly Stack<NavigationItemViewModel> _forwardHistory = new();
    private readonly List<SearchSuggestionViewModel> _searchCatalog = [];
    private readonly RelayCommand _navigateBackCommand;
    private readonly RelayCommand _navigateForwardCommand;
    private readonly RelayCommand _navigateHomeCommand;
    private NavigationItemViewModel? _selectedNavigationItem;
    private ViewModelBase? _currentViewModel;
    private string _statusMessage = "Listo para ejecutar acciones seguras.";
    private string _searchText = string.Empty;
    private bool _isAdmin;
    private bool _isBusy;
    private bool _isExpertMode;
    private bool _isNavigatingHistory;
    private bool _isSearchOpen;

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
            new NavigationItemViewModel("Diagnóstico", "DG", "\uE9D9", "diagnostico", Diagnostic),
            new NavigationItemViewModel("Gaming", "GM", "\uE7FC", "gaming", Gaming),
            new NavigationItemViewModel("Red", "RD", "\uE968", "red", Network),
            new NavigationItemViewModel("Streaming", "ST", "\uE714", "streaming", Streaming),
            new NavigationItemViewModel("Procesos", "PR", "\uE9D2", "procesos", Processes),
            new NavigationItemViewModel("Privacidad", "PV", "\uE72E", "privacidad", Privacy),
            new NavigationItemViewModel("Backups", "BK", "\uE74E", "backups", Backups),
            new NavigationItemViewModel("Ajustes", "AJ", "\uE713", "ajustes", Settings),
            new NavigationItemViewModel("Acerca de", "AC", "\uE946", "acerca-de", About)
        ];

        SearchSuggestions = [];
        InitializeCommand = new AsyncRelayCommand(InitializeAsync);
        RefreshCommand = new AsyncRelayCommand(RefreshCurrentAsync);
        ExportReportCommand = new AsyncRelayCommand(ExportReportAsync);
        RestartAsAdminCommand = new RelayCommand(RestartAsAdmin);
        ToggleModeCommand = new AsyncRelayCommand(ToggleModeAsync);
        NavigateBackCommand = _navigateBackCommand = new RelayCommand(NavigateBack, () => _backHistory.Count > 0);
        NavigateForwardCommand = _navigateForwardCommand = new RelayCommand(NavigateForward, () => _forwardHistory.Count > 0);
        NavigateHomeCommand = _navigateHomeCommand = new RelayCommand(NavigateHome, () => SelectedNavigationItem != NavigationItems[0]);
        NavigateSearchCommand = new RelayCommand(NavigateToSearchSuggestion, parameter => parameter is SearchSuggestionViewModel);

        BuildSearchCatalog();
        SelectedNavigationItem = NavigationItems[0];
    }

    public ObservableCollection<NavigationItemViewModel> NavigationItems { get; }
    public ObservableCollection<SearchSuggestionViewModel> SearchSuggestions { get; }
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
    public ICommand RestartAsAdminCommand { get; }
    public ICommand ToggleModeCommand { get; }
    public ICommand NavigateBackCommand { get; }
    public ICommand NavigateForwardCommand { get; }
    public ICommand NavigateHomeCommand { get; }
    public ICommand NavigateSearchCommand { get; }

    public NavigationItemViewModel? SelectedNavigationItem
    {
        get => _selectedNavigationItem;
        set
        {
            if (value is null || ReferenceEquals(_selectedNavigationItem, value))
            {
                return;
            }

            var previous = _selectedNavigationItem;
            if (SetProperty(ref _selectedNavigationItem, value))
            {
                if (!_isNavigatingHistory && previous is not null)
                {
                    _backHistory.Push(previous);
                    _forwardHistory.Clear();
                }

                CurrentViewModel = value.ViewModel;
                OnPropertyChanged(nameof(CurrentAddress));
                UpdateHistoryCommands();
                _ = RefreshCurrentAsync();
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

    public string CurrentAddress => SelectedNavigationItem?.Address ?? "nokara://dashboard";

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                UpdateSearchSuggestions();
            }
        }
    }

    public bool IsSearchOpen
    {
        get => _isSearchOpen;
        private set => SetProperty(ref _isSearchOpen, value);
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

    public string AdminDisplay => IsAdmin ? "Administrador: Sí" : "Administrador: No";

    public bool IsExpertMode
    {
        get => _isExpertMode;
        private set
        {
            if (SetProperty(ref _isExpertMode, value))
            {
                OnPropertyChanged(nameof(ModeDisplay));
                OnPropertyChanged(nameof(ModeHint));
            }
        }
    }

    public string ModeDisplay => IsExpertMode ? "Modo avanzado" : "Modo básico";
    public string ModeHint => IsExpertMode ? "Muestra acciones medias y avanzadas con confirmación." : "Prioriza acciones seguras y comunes.";

    public async Task InitializeAsync()
    {
        IsBusy = true;
        try
        {
            await _services.Settings.EnsureKnownProcessesAsync();
            var settings = await _services.Settings.LoadAsync();
            IsExpertMode = settings.ExpertMode;
            IsAdmin = _services.Admin.IsRunningAsAdmin();
            await Dashboard.LoadAsync();
            await Maintenance.LoadAsync();
            await About.LoadAsync();
            StatusMessage = "Listo para ejecutar acciones seguras.";
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

    private void RestartAsAdmin()
    {
        if (_services.Admin.RestartAsAdmin())
        {
            Application.Current.Shutdown();
        }
    }

    private async Task ToggleModeAsync()
    {
        var settings = await _services.Settings.LoadAsync();
        settings.ExpertMode = !settings.ExpertMode;
        await _services.Settings.SaveAsync(settings);
        IsExpertMode = settings.ExpertMode;
        StatusMessage = IsExpertMode ? "Modo avanzado activo. Las acciones sensibles siguen pidiendo confirmación." : "Modo básico activo. Usa primero acciones de bajo riesgo.";
        await Settings.LoadAsync();
    }

    private void SelectSection(string section)
    {
        if (string.IsNullOrWhiteSpace(section))
        {
            return;
        }

        var item = NavigationItems.FirstOrDefault(n => string.Equals(n.Title, section, StringComparison.OrdinalIgnoreCase));
        if (item is not null)
        {
            SelectedNavigationItem = item;
        }
    }

    private void NavigateBack()
    {
        if (_backHistory.Count == 0 || SelectedNavigationItem is null)
        {
            return;
        }

        _forwardHistory.Push(SelectedNavigationItem);
        NavigateFromHistory(_backHistory.Pop());
    }

    private void NavigateForward()
    {
        if (_forwardHistory.Count == 0 || SelectedNavigationItem is null)
        {
            return;
        }

        _backHistory.Push(SelectedNavigationItem);
        NavigateFromHistory(_forwardHistory.Pop());
    }

    private void NavigateHome()
    {
        SelectedNavigationItem = NavigationItems[0];
    }

    private void NavigateFromHistory(NavigationItemViewModel item)
    {
        _isNavigatingHistory = true;
        try
        {
            SelectedNavigationItem = item;
        }
        finally
        {
            _isNavigatingHistory = false;
            UpdateHistoryCommands();
        }
    }

    private void UpdateHistoryCommands()
    {
        _navigateBackCommand.RaiseCanExecuteChanged();
        _navigateForwardCommand.RaiseCanExecuteChanged();
        _navigateHomeCommand.RaiseCanExecuteChanged();
    }

    private void BuildSearchCatalog()
    {
        _searchCatalog.Clear();
        var maintenance = NavigationItems.First(n => n.RouteSegment == "mantenimiento");
        var diagnostic = NavigationItems.First(n => n.RouteSegment == "diagnostico");
        var gaming = NavigationItems.First(n => n.RouteSegment == "gaming");
        var network = NavigationItems.First(n => n.RouteSegment == "red");
        var processes = NavigationItems.First(n => n.RouteSegment == "procesos");
        var backups = NavigationItems.First(n => n.RouteSegment == "backups");

        AddSearch("Flush DNS", "Ir a Red para limpiar caché DNS.", "\uE968", network, "dns", "red", "internet");
        AddSearch("Limpiar TEMP", "Ir a Mantenimiento para temporales seguros.", "\uE90F", maintenance, "temp", "limpieza", "cache");
        AddSearch("Diagnóstico completo", "Revisar stuttering, RAM, red y almacenamiento.", "\uE9D9", diagnostic, "diagnostico", "stuttering", "almacenamiento");
        AddSearch("Gaming / FPS", "Optimización segura para juego y FPS.", "\uE7FC", gaming, "fps", "juego", "game mode");
        AddSearch("Stuttering", "Analizar tirones, overlays y procesos pesados.", "\uE9D9", diagnostic, "tirones", "wallpaper", "overlays");
        AddSearch("RAM", "Ver presión de memoria y procesos grandes.", "\uE9D2", processes, "memoria", "procesos pesados");
        AddSearch("Procesos", "Administrar prioridades seguras.", "\uE9D2", processes, "prioridad", "above normal", "high");
        AddSearch("Backups", "Ver copias y reversión de cambios.", "\uE74E", backups, "restaurar", "revertir");
        AddSearch("Reporte", "Crear reporte final desde la barra superior.", "\uE8A5", diagnostic, "exportar", "txt", "notepad");
        AddSearch("Red", "Adaptadores, DNS y benchmark.", "\uE968", network, "benchmark", "adaptador");
        AddSearch("Almacenamiento", "Espacio libre y chkdsk /scan seguro.", "\uE9D9", diagnostic, "disco", "chkdsk", "scan");
    }

    private void AddSearch(string title, string description, string iconGlyph, NavigationItemViewModel target, params string[] keywords)
    {
        _searchCatalog.Add(new SearchSuggestionViewModel(title, description, iconGlyph, target, keywords));
    }

    private void UpdateSearchSuggestions()
    {
        SearchSuggestions.Clear();
        var query = SearchText.Trim();
        if (query.Length < 2)
        {
            IsSearchOpen = false;
            return;
        }

        foreach (var suggestion in _searchCatalog.Where(s => s.Matches(query)).Take(7))
        {
            SearchSuggestions.Add(suggestion);
        }

        IsSearchOpen = SearchSuggestions.Count > 0;
    }

    private void NavigateToSearchSuggestion(object? parameter)
    {
        if (parameter is not SearchSuggestionViewModel suggestion)
        {
            return;
        }

        SelectedNavigationItem = suggestion.Target;
        StatusMessage = $"Búsqueda: {suggestion.Title}";
        SearchText = string.Empty;
        IsSearchOpen = false;
    }
}
