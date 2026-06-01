using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using NokaraSystemManager.Helpers;
using NokaraSystemManager.Models;
using NokaraSystemManager.Services;

namespace NokaraSystemManager.ViewModels;

public sealed class ProcessesViewModel : ViewModelBase
{
    private readonly AppServices _services;
    private readonly Func<OptimizationAction, ActionResult, Task> _onCompleted;
    private ProcessInfoItem? _selectedProcess;
    private ProcessPriorityClass _selectedPriority = ProcessPriorityClass.AboveNormal;
    private string _status = "Procesos listos.";
    private string _selectedFilter = "Todos";

    public ProcessesViewModel(AppServices services, Func<OptimizationAction, ActionResult, Task> onCompleted)
    {
        _services = services;
        _onCompleted = onCompleted;
        PriorityOptions = [ProcessPriorityClass.Idle, ProcessPriorityClass.BelowNormal, ProcessPriorityClass.Normal, ProcessPriorityClass.AboveNormal, ProcessPriorityClass.High];
        FilterOptions = ["Todos", "Juegos", "Launchers", "Streaming", "Comunicación", "Navegadores", "Multimedia", "Pesados"];
        RefreshCommand = new AsyncRelayCommand(LoadAsync);
        ApplyPriorityCommand = new AsyncRelayCommand(ApplyPriorityAsync, () => SelectedProcess is not null);
    }

    public ObservableCollection<ProcessInfoItem> Processes { get; } = [];
    public ObservableCollection<ProcessInfoItem> FilteredProcesses { get; } = [];
    public ObservableCollection<ProcessPriorityClass> PriorityOptions { get; }
    public ObservableCollection<string> FilterOptions { get; }
    public ICommand RefreshCommand { get; }
    public ICommand ApplyPriorityCommand { get; }

    public ProcessInfoItem? SelectedProcess
    {
        get => _selectedProcess;
        set => SetProperty(ref _selectedProcess, value);
    }

    public ProcessPriorityClass SelectedPriority
    {
        get => _selectedPriority;
        set => SetProperty(ref _selectedPriority, value);
    }

    public string SelectedFilter
    {
        get => _selectedFilter;
        set
        {
            if (SetProperty(ref _selectedFilter, value))
            {
                ApplyFilter();
            }
        }
    }

    public string Status
    {
        get => _status;
        private set => SetProperty(ref _status, value);
    }

    public int TotalProcesses => Processes.Count;
    public int HeavyProcessCount => Processes.Count(p => p.IsHighMemory);
    public int GameProcessCount => Processes.Count(p => p.Category == ProcessCategory.Game);
    public int StreamingProcessCount => Processes.Count(p => p.Category == ProcessCategory.Streaming);

    public async Task LoadAsync()
    {
        Processes.Clear();
        foreach (var item in await _services.Processes.GetProcessesAsync())
        {
            Processes.Add(item);
        }

        ApplyFilter();
        SelectedProcess ??= FilteredProcesses.FirstOrDefault();
        OnPropertyChanged(nameof(TotalProcesses));
        OnPropertyChanged(nameof(HeavyProcessCount));
        OnPropertyChanged(nameof(GameProcessCount));
        OnPropertyChanged(nameof(StreamingProcessCount));
        Status = $"{Processes.Count} procesos detectados. Filtro: {SelectedFilter}.";
    }

    private async Task ApplyPriorityAsync()
    {
        if (SelectedProcess is null)
        {
            return;
        }

        var result = await _services.ProcessPriority.SetPriorityAsync(SelectedProcess, SelectedPriority);
        var action = new OptimizationAction { Id = "process.priority", Name = "Cambiar prioridad", Module = "Procesos", RiskLevel = RiskLevel.Medium };
        await _services.Backup.AddActionHistoryAsync(action.Module, action.Id, $"{action.Name}: {SelectedProcess.ProcessName}", result);
        await _services.Logging.LogActionResultAsync(action.Module, $"{action.Name}: {SelectedProcess.ProcessName}", result);
        Status = result.Message;
        await _onCompleted(action, result);
        await LoadAsync();
    }

    private void ApplyFilter()
    {
        IEnumerable<ProcessInfoItem> query = Processes;
        query = SelectedFilter switch
        {
            "Juegos" => query.Where(p => p.Category == ProcessCategory.Game),
            "Launchers" => query.Where(p => p.Category == ProcessCategory.GameLauncher),
            "Streaming" => query.Where(p => p.Category == ProcessCategory.Streaming),
            "Comunicación" => query.Where(p => p.Category == ProcessCategory.Communication),
            "Navegadores" => query.Where(p => p.Category == ProcessCategory.Browser),
            "Multimedia" => query.Where(p => p.Category == ProcessCategory.Multimedia),
            "Pesados" => query.Where(p => p.IsHighMemory),
            _ => query.Where(p => !p.IsCritical)
        };

        FilteredProcesses.Clear();
        foreach (var item in query.OrderByDescending(p => p.MemoryMB))
        {
            FilteredProcesses.Add(item);
        }

        SelectedProcess = FilteredProcesses.FirstOrDefault();
        Status = $"{FilteredProcesses.Count} procesos visibles de {Processes.Count}.";
    }
}
