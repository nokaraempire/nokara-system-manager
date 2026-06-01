using NokaraSystemManager.Models;

namespace NokaraSystemManager.Services;

public sealed class ProfileService
{
    public IReadOnlyList<ProfileStep> CreateGamingProfile() =>
    [
        new ProfileStep { Id = "gaming.game-mode", Name = "Activar Game Mode", Module = "Gaming", Description = "Prioriza recursos para juegos compatibles.", RiskLevel = RiskLevel.Low },
        new ProfileStep { Id = "gaming.game-dvr", Name = "Desactivar Game DVR", Module = "Gaming", Description = "Reduce capturas en segundo plano y overlays innecesarios.", RiskLevel = RiskLevel.Low },
        new ProfileStep { Id = "gaming.power-plan", Name = "Plan Alto rendimiento", Module = "Energia", Description = "Activa el plan Alto rendimiento de Windows.", RiskLevel = RiskLevel.Medium },
        new ProfileStep { Id = "network.flush-dns", Name = "Limpiar caché DNS", Module = "Red", Description = "Refresca resoluciones DNS antes de jugar.", RiskLevel = RiskLevel.Low },
        new ProfileStep { Id = "maintenance.user-temp", Name = "Limpiar TEMP usuario", Module = "Mantenimiento", Description = "Elimina temporales no críticos del usuario.", RiskLevel = RiskLevel.Low }
    ];

    public IReadOnlyList<ProfileStep> CreateStreamingProfile() =>
    [
        new ProfileStep { Id = "gaming.game-mode", Name = "Activar Game Mode", Module = "Gaming", Description = "Mantiene modo juego activo durante captura.", RiskLevel = RiskLevel.Low },
        new ProfileStep { Id = "gaming.transparency", Name = "Reducir transparencias", Module = "Gaming", Description = "Disminuye carga visual del escritorio.", RiskLevel = RiskLevel.Low },
        new ProfileStep { Id = "network.flush-dns", Name = "Limpiar caché DNS", Module = "Red", Description = "Evita resoluciones DNS antiguas.", RiskLevel = RiskLevel.Low },
        new ProfileStep { Id = "privacy.tips", Name = "Ocultar tips", Module = "Privacidad", Description = "Reduce interrupciones visuales de Windows.", RiskLevel = RiskLevel.Low }
    ];

    public IReadOnlyList<ProfileStep> CreateRestoreProfile() =>
    [
        new ProfileStep { Id = "restore.power-plan", Name = "Restaurar plan energía", Module = "Backups", Description = "Vuelve al plan de energía guardado más reciente.", RiskLevel = RiskLevel.Low },
        new ProfileStep { Id = "restore.registry", Name = "Revisar backups de registro", Module = "Backups", Description = "Los cambios reversibles se restauran desde la seccion Backups.", RiskLevel = RiskLevel.Low, IsOptional = true }
    ];

    public IReadOnlyList<ProfileStep> CreateDiagnosticProfile() =>
    [
        new ProfileStep { Id = "diagnostic.system", Name = "Analizar sistema", Module = "Diagnóstico", Description = "Revisa Windows, admin, CPU, RAM, GPU y plan de energía sin aplicar cambios.", RiskLevel = RiskLevel.Low },
        new ProfileStep { Id = "diagnostic.processes", Name = "Analizar procesos", Module = "Diagnóstico", Description = "Detecta juegos, launchers, streaming, navegadores, overlays y procesos pesados.", RiskLevel = RiskLevel.Low },
        new ProfileStep { Id = "diagnostic.storage", Name = "Analizar almacenamiento", Module = "Diagnóstico", Description = "Revisa espacio libre y unidades detectadas. chkdsk /scan queda como acción manual confirmada.", RiskLevel = RiskLevel.Low },
        new ProfileStep { Id = "diagnostic.network", Name = "Analizar red", Module = "Diagnóstico", Description = "Muestra adaptador activo, gateway y DNS actual.", RiskLevel = RiskLevel.Low },
        new ProfileStep { Id = "diagnostic.report", Name = "Generar reporte", Module = "Diagnóstico", Description = "Crea reporte final en Escritorio y AppData Reports.", RiskLevel = RiskLevel.Low }
    ];
}
