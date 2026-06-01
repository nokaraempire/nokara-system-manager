using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using NokaraSystemManager.Helpers;
using NokaraSystemManager.Models;
using NokaraSystemManager.Services;

namespace NokaraSystemManager.ViewModels;

public sealed class DiagnosticViewModel : ViewModelBase
{
    private readonly AppServices _services;
    private DiagnosticSnapshot _snapshot = new();
    private string _status = "Diagnóstico listo.";
    private string _selectedDetail = "Ejecutá Analizar para actualizar el diagnóstico completo.";
    private string _lastReportPath = string.Empty;

    public DiagnosticViewModel(AppServices services)
    {
        _services = services;
        AnalyzeCommand = new AsyncRelayCommand(LoadAsync);
        GenerateReportCommand = new AsyncRelayCommand(GenerateReportAsync);
        StorageScanCommand = new AsyncRelayCommand(RunStorageScanAsync);
        AnalyzeCardCommand = new RelayCommand(AnalyzeCard);
        ApplyCardCommand = new RelayCommand(card => _ = ApplyCardAsync(card));
    }

    public ObservableCollection<DiagnosticCard> Cards { get; } = [];
    public ObservableCollection<DiagnosticIssue> Issues { get; } = [];
    public ObservableCollection<ProcessInfoItem> TopRamProcesses { get; } = [];
    public ObservableCollection<StorageInfoItem> Storage { get; } = [];
    public ObservableCollection<ManualGamePath> ManualPaths { get; } = [];

    public ICommand AnalyzeCommand { get; }
    public ICommand GenerateReportCommand { get; }
    public ICommand StorageScanCommand { get; }
    public ICommand AnalyzeCardCommand { get; }
    public ICommand ApplyCardCommand { get; }

    public DiagnosticSnapshot Snapshot
    {
        get => _snapshot;
        private set => SetProperty(ref _snapshot, value);
    }

    public string Status
    {
        get => _status;
        private set => SetProperty(ref _status, value);
    }

    public string SelectedDetail
    {
        get => _selectedDetail;
        private set => SetProperty(ref _selectedDetail, value);
    }

    public string LastReportPath
    {
        get => _lastReportPath;
        private set => SetProperty(ref _lastReportPath, value);
    }

    public async Task LoadAsync()
    {
        Snapshot = await _services.Diagnostics.AnalyzeAsync();
        Cards.Clear();
        foreach (var card in _services.Diagnostics.BuildCards(Snapshot))
        {
            Cards.Add(card);
        }

        Issues.Clear();
        foreach (var issue in Snapshot.Issues.OrderByDescending(i => i.Severity))
        {
            Issues.Add(issue);
        }

        TopRamProcesses.Clear();
        foreach (var process in Snapshot.Processes.TopProcessesByRam.Take(8))
        {
            TopRamProcesses.Add(process);
        }

        Storage.Clear();
        foreach (var item in Snapshot.Storage)
        {
            Storage.Add(item);
        }

        ManualPaths.Clear();
        foreach (var item in Snapshot.ManualPaths)
        {
            ManualPaths.Add(item);
        }

        Status = $"Diagnóstico actualizado. {Issues.Count} punto(s) a revisar.";
        SelectedDetail = Issues.Count == 0
            ? "No se detectaron alertas fuertes. Generá un reporte si querés dejar registro del estado actual."
            : string.Join(Environment.NewLine, Issues.Take(5).Select(i => $"{i.Title}: {i.SuggestedAction}"));
    }

    private void AnalyzeCard(object? parameter)
    {
        if (parameter is not DiagnosticCard card)
        {
            return;
        }

        SelectedDetail = $"{card.Title}{Environment.NewLine}{card.Explanation}{Environment.NewLine}{Environment.NewLine}Problemas: {card.Problems}{Environment.NewLine}Sugerido: {card.SuggestedActions}{Environment.NewLine}Riesgo: {card.RiskLabel}{Environment.NewLine}Admin: {(card.RequiresAdmin ? "Sí" : "No")}{Environment.NewLine}Reversible: {(card.IsReversible ? "Sí" : "No")}";
    }

    private async Task ApplyCardAsync(object? parameter)
    {
        if (parameter is not DiagnosticCard card)
        {
            return;
        }

        if (card.Key == "report")
        {
            await GenerateReportAsync();
            return;
        }

        SelectedDetail = string.IsNullOrWhiteSpace(card.DisabledReason)
            ? "Esta card no aplica cambios directos. Usá las secciones Gaming, Red, Procesos o Backups para ejecutar acciones con confirmación."
            : card.DisabledReason;
    }

    private async Task GenerateReportAsync()
    {
        if (Cards.Count == 0)
        {
            await LoadAsync();
        }

        LastReportPath = await _services.Reports.GenerateSystemReportAsync("Diagnóstico completo", openNotepad: true, Snapshot);
        Status = $"Reporte generado: {LastReportPath}";
        SelectedDetail = $"Reporte final creado en Escritorio y copiado en AppData Reports:{Environment.NewLine}{LastReportPath}";
    }

    private async Task RunStorageScanAsync()
    {
        var root = Storage.FirstOrDefault()?.Root ?? Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\";
        var message = $"Se ejecutará: chkdsk {root.TrimEnd('\\')} /scan{Environment.NewLine}{Environment.NewLine}No se usará /f ni /r. No se reparará nada automáticamente.";
        if (MessageBox.Show(message, "Buscar errores de almacenamiento", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
        {
            var cancel = ActionResult.Cancel("Buscar errores de almacenamiento");
            await _services.Backup.AddActionHistoryAsync("Diagnóstico", "storage.scan", "Buscar errores de almacenamiento", cancel);
            await _services.Logging.LogActionResultAsync("Diagnóstico", "Buscar errores de almacenamiento", cancel);
            Status = "Comprobación de almacenamiento cancelada.";
            return;
        }

        var result = await _services.Diagnostics.RunStorageScanAsync(root);
        await _services.Backup.AddActionHistoryAsync("Diagnóstico", "storage.scan", "Buscar errores de almacenamiento", result);
        await _services.Logging.LogActionResultAsync("Diagnóstico", "Buscar errores de almacenamiento", result);
        Status = result.Message;
        SelectedDetail = string.IsNullOrWhiteSpace(result.Details) ? result.Message : result.Details;
        await LoadAsync();
    }
}
