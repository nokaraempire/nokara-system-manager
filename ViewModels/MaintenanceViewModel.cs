using System.Collections.ObjectModel;
using System.Windows.Input;
using NokaraSystemManager.Helpers;
using NokaraSystemManager.Models;
using NokaraSystemManager.Services;

namespace NokaraSystemManager.ViewModels;

public sealed class MaintenanceViewModel : ViewModelBase
{
    private readonly AppServices _services;

    public MaintenanceViewModel(AppServices services, Func<OptimizationAction, ActionCardViewModel> cardFactory)
    {
        _services = services;
        RefreshEstimateCommand = new AsyncRelayCommand(LoadAsync);
        Actions =
        [
            cardFactory(new OptimizationAction { Id = "maintenance.analyze", Name = "Analizar espacio liberable", Description = "Calcula temporales y cachés sin borrar nada.", Module = "Mantenimiento", RiskLevel = RiskLevel.Low, WhenToUse = "Antes de limpiar o cuando querés saber cuánto se puede liberar.", ExpectedImpact = "Solo lectura. Sin cambios en disco.", CurrentState = "Seguro", ExecuteAsync = AnalyzeSpaceAsync }),
            cardFactory(new OptimizationAction { Id = "maintenance.user-temp", Name = "Limpiar TEMP usuario", Description = "Elimina archivos temporales del usuario sin tocar documentos ni descargas.", Module = "Mantenimiento", RiskLevel = RiskLevel.Low, IsReversible = false, WhenToUse = "Después de instalar apps, actualizar juegos o cuando falta espacio.", ExpectedImpact = "Libera espacio chico/medio. Archivos en uso se omiten.", CurrentState = "Seguro", ExecuteAsync = services.Cleaner.CleanUserTempAsync }),
            cardFactory(new OptimizationAction { Id = "maintenance.windows-temp", Name = "Limpiar TEMP Windows", Description = "Borra temporales de Windows que no estén en uso.", Module = "Mantenimiento", RiskLevel = RiskLevel.Medium, RequiresAdmin = true, WhenToUse = "Cuando Windows Temp ocupa demasiado o hay instaladores viejos.", ExpectedImpact = "Puede liberar espacio. Requiere permisos elevados.", CurrentState = "Requiere admin", IsAdvanced = true, ExecuteAsync = services.Cleaner.CleanWindowsTempAsync }),
            cardFactory(new OptimizationAction { Id = "maintenance.flush-dns", Name = "Flush DNS", Description = "Limpia la caché DNS local sin cambiar configuración de red.", Module = "Mantenimiento", RiskLevel = RiskLevel.Low, WhenToUse = "Si un sitio/juego resuelve mal o cambiaste DNS recientemente.", ExpectedImpact = "Acción rápida, no corta internet.", CurrentState = "Seguro", ExecuteAsync = services.Network.FlushDnsAsync }),
            cardFactory(new OptimizationAction { Id = "maintenance.thumbnails", Name = "Limpiar miniaturas", Description = "Elimina caché de miniaturas; Windows la reconstruye sola.", Module = "Mantenimiento", RiskLevel = RiskLevel.Low, WhenToUse = "Si el explorador muestra miniaturas rotas o querés liberar algo de espacio.", ExpectedImpact = "Primeras carpetas pueden tardar en regenerar miniaturas.", CurrentState = "Seguro", ExecuteAsync = services.Cleaner.CleanThumbnailCacheAsync }),
            cardFactory(new OptimizationAction { Id = "maintenance.dx-cache", Name = "Limpiar DirectX Shader Cache", Description = "Borra caché de shaders DirectX.", Module = "Mantenimiento", RiskLevel = RiskLevel.Low, WhenToUse = "Tras cambiar driver GPU o si un juego compila shaders con errores.", ExpectedImpact = "Puede causar microcarga inicial al abrir juegos de nuevo.", CurrentState = "Seguro con advertencia", ExecuteAsync = services.Cleaner.CleanDirectXShaderCacheAsync }),
            cardFactory(new OptimizationAction { Id = "maintenance.recycle-bin", Name = "Vaciar papelera", Description = "Vacía la papelera sin tocar otros archivos.", Module = "Mantenimiento", RiskLevel = RiskLevel.Medium, WhenToUse = "Cuando ya revisaste que no necesitás recuperar lo borrado.", ExpectedImpact = "Libera espacio pero no es reversible desde Nokara.", CurrentState = "Confirmación recomendada", ExecuteAsync = services.Cleaner.EmptyRecycleBinAsync }),
            cardFactory(new OptimizationAction { Id = "maintenance.old-logs", Name = "Limpiar logs viejos", Description = "Borra logs de Nokara con más de 30 días.", Module = "Mantenimiento", RiskLevel = RiskLevel.Low, WhenToUse = "Mantenimiento liviano de la propia app.", ExpectedImpact = "Libera poco espacio y conserva logs recientes.", CurrentState = "Seguro", ExecuteAsync = services.Cleaner.CleanOldTemporaryLogsAsync }),
            cardFactory(new OptimizationAction { Id = "maintenance.windows-update", Name = "Caché Windows Update", Description = "Pendiente: limpieza segura de Windows Update requiere detener servicios y validar reversión.", Module = "Mantenimiento", RiskLevel = RiskLevel.High, RequiresAdmin = true, IsImplemented = false, IsEnabled = false, IsAdvanced = true, WhenToUse = "Solo cuando Windows Update falla y hay procedimiento seguro.", ExpectedImpact = "Puede afectar actualizaciones si se hace mal.", CurrentState = "Pendiente", DisabledReason = "Deshabilitado en V1.0.4: no se detienen servicios de Windows Update ni se borran componentes críticos." })
        ];
    }

    public ObservableCollection<ActionCardViewModel> Actions { get; }
    public ObservableCollection<CleanupEstimateItem> Estimates { get; } = [];
    public ICommand RefreshEstimateCommand { get; }

    public async Task LoadAsync()
    {
        Estimates.Clear();
        foreach (var item in await _services.Cleaner.EstimateCleanableSpaceAsync())
        {
            Estimates.Add(item);
        }
    }

    private async Task<ActionResult> AnalyzeSpaceAsync(CancellationToken cancellationToken)
    {
        var items = await _services.Cleaner.EstimateCleanableSpaceAsync(cancellationToken);
        var total = items.Sum(i => i.Bytes);
        return ActionResult.Ok("Analizar espacio liberable", $"Espacio estimado: {FormatHelper.Bytes(total)}", string.Join(Environment.NewLine, items.Select(i => $"{i.Name}: {i.SizeDisplay}")));
    }
}
