using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using NokaraSystemManager.Helpers;
using NokaraSystemManager.Models;
using NokaraSystemManager.Services;

namespace NokaraSystemManager.ViewModels;

public sealed class NetworkViewModel : ViewModelBase
{
    private readonly AppServices _services;
    private readonly Func<OptimizationAction, ActionResult, Task> _onCompleted;
    private NetworkAdapterInfo? _selectedAdapter;
    private DnsProvider? _selectedProvider;
    private string _status = "Red lista.";
    private bool _isBenchmarking;

    public NetworkViewModel(AppServices services, Func<OptimizationAction, ActionResult, Task> onCompleted)
    {
        _services = services;
        _onCompleted = onCompleted;
        RefreshCommand = new AsyncRelayCommand(LoadAsync);
        FlushDnsCommand = new AsyncRelayCommand(() => RunAsync("network.flush-dns", "Limpiar caché DNS", services.Network.FlushDnsAsync, risk: RiskLevel.Low));
        ReleaseRenewCommand = new AsyncRelayCommand(() => RunAsync("network.release-renew", "Release/Renew IP", services.Network.ReleaseRenewIpAsync, risk: RiskLevel.Medium));
        ResetWinsockCommand = new AsyncRelayCommand(() => RunAsync("network.winsock", "Reset Winsock", services.Network.ResetWinsockAsync, risk: RiskLevel.High));
        ResetTcpIpCommand = new AsyncRelayCommand(() => RunAsync("network.tcpip", "Reset TCP/IP", services.Network.ResetTcpIpAsync, risk: RiskLevel.High));
        ApplyDnsCommand = new AsyncRelayCommand(ApplyDnsAsync, () => SelectedAdapter is not null && SelectedProvider is not null);
        RestoreDnsAutomaticCommand = new AsyncRelayCommand(RestoreDnsAutomaticAsync, () => SelectedAdapter is not null);
        BenchmarkDnsCommand = new AsyncRelayCommand(BenchmarkDnsAsync, () => !IsBenchmarking);
    }

    public ObservableCollection<NetworkAdapterInfo> Adapters { get; } = [];
    public ObservableCollection<DnsProvider> DnsProviders { get; } = [];
    public ObservableCollection<DnsTestResult> DnsResults { get; } = [];
    public ICommand RefreshCommand { get; }
    public ICommand FlushDnsCommand { get; }
    public ICommand ReleaseRenewCommand { get; }
    public ICommand ResetWinsockCommand { get; }
    public ICommand ResetTcpIpCommand { get; }
    public ICommand ApplyDnsCommand { get; }
    public ICommand RestoreDnsAutomaticCommand { get; }
    public ICommand BenchmarkDnsCommand { get; }

    public NetworkAdapterInfo? SelectedAdapter
    {
        get => _selectedAdapter;
        set => SetProperty(ref _selectedAdapter, value);
    }

    public DnsProvider? SelectedProvider
    {
        get => _selectedProvider;
        set => SetProperty(ref _selectedProvider, value);
    }

    public string Status
    {
        get => _status;
        private set => SetProperty(ref _status, value);
    }

    public bool IsBenchmarking
    {
        get => _isBenchmarking;
        private set => SetProperty(ref _isBenchmarking, value);
    }

    public Task LoadAsync()
    {
        Adapters.Clear();
        foreach (var adapter in _services.Network.GetAdapters())
        {
            Adapters.Add(adapter);
        }

        DnsProviders.Clear();
        foreach (var provider in _services.Network.GetDnsProviders())
        {
            DnsProviders.Add(provider);
        }

        SelectedAdapter ??= Adapters.FirstOrDefault(a => a.IsActive) ?? Adapters.FirstOrDefault();
        SelectedProvider ??= DnsProviders.FirstOrDefault();
        Status = Adapters.Count == 0 ? "No se detectaron adaptadores disponibles." : "Adaptadores actualizados.";
        return Task.CompletedTask;
    }

    private async Task ApplyDnsAsync()
    {
        if (SelectedAdapter is null || SelectedProvider is null)
        {
            return;
        }

        await RunAsync("network.dns", $"DNS: {SelectedProvider.Name}", ct => _services.Network.SetDnsManualAsync(SelectedAdapter, SelectedProvider, ct), requiresAdmin: true, risk: RiskLevel.Medium);
        await LoadAsync();
    }

    private async Task RestoreDnsAutomaticAsync()
    {
        if (SelectedAdapter is null)
        {
            return;
        }

        await RunAsync("network.dns-auto", "Restaurar DNS automático", ct => _services.Network.SetDnsAutomaticAsync(SelectedAdapter, ct), requiresAdmin: true, risk: RiskLevel.Medium);
        await LoadAsync();
    }

    private async Task BenchmarkDnsAsync()
    {
        IsBenchmarking = true;
        try
        {
            Status = "Probando DNS...";
            DnsResults.Clear();
            var results = await _services.DnsBenchmark.BenchmarkAsync(DnsProviders);
            foreach (var result in results)
            {
                DnsResults.Add(result);
            }

            Status = DnsResults.Count == 0 ? "No hubo resultados DNS." : "Benchmark DNS completado.";
        }
        finally
        {
            IsBenchmarking = false;
        }
    }

    private async Task RunAsync(string id, string name, Func<CancellationToken, Task<ActionResult>> execute, bool requiresAdmin = false, RiskLevel risk = RiskLevel.Medium)
    {
        var action = new OptimizationAction { Id = id, Name = name, Module = "Red", RiskLevel = risk, RequiresAdmin = requiresAdmin };
        ActionResult result;
        if (requiresAdmin && !_services.Admin.IsRunningAsAdmin())
        {
            result = ActionResult.Fail(name, "Requiere ejecutar como administrador.");
            result.RequiresAdmin = true;
        }
        else if (!await ConfirmIfNeededAsync(name, risk))
        {
            result = ActionResult.Cancel(name);
        }
        else
        {
            result = await execute(CancellationToken.None);
        }

        Status = result.Message;
        await _services.Backup.AddActionHistoryAsync(action.Module, action.Id, action.Name, result);
        await _services.Logging.LogActionResultAsync(action.Module, action.Name, result);
        await _onCompleted(action, result);
    }

    private async Task<bool> ConfirmIfNeededAsync(string name, RiskLevel risk)
    {
        var settings = await _services.Settings.LoadAsync();
        if (!settings.ExtraConfirmations || risk == RiskLevel.Low)
        {
            return true;
        }

        return MessageBox.Show($"{name}\n\nEsta acción puede cortar internet temporalmente o requerir reinicio según el caso.\n\n¿Querés continuar?", "Confirmar acción de red", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;
    }
}
