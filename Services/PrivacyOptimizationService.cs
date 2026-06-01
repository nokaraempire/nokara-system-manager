using Microsoft.Win32;
using NokaraSystemManager.Models;

namespace NokaraSystemManager.Services;

public sealed class PrivacyOptimizationService
{
    private readonly RegistryService _registry;

    public PrivacyOptimizationService(RegistryService registry)
    {
        _registry = registry;
    }

    public async Task<ActionResult> DisableAdvertisingIdAsync(CancellationToken ct = default) => await _registry.SetValueWithBackupAsync(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo", "Enabled", 0, RegistryValueKind.DWord, "Privacidad", "privacy.ad-id", "Desactivar Advertising ID", ct);
    public async Task<ActionResult> DisableStartSuggestionsAsync(CancellationToken ct = default) => await _registry.SetValueWithBackupAsync(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SubscribedContent-338388Enabled", 0, RegistryValueKind.DWord, "Privacidad", "privacy.start-suggestions", "Desactivar sugerencias Inicio", ct);
    public async Task<ActionResult> DisableTipsAsync(CancellationToken ct = default) => await _registry.SetValueWithBackupAsync(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SoftLandingEnabled", 0, RegistryValueKind.DWord, "Privacidad", "privacy.tips", "Desactivar tips", ct);
    public async Task<ActionResult> DisableTailoredExperiencesAsync(CancellationToken ct = default) => await _registry.SetValueWithBackupAsync(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Privacy", "TailoredExperiencesWithDiagnosticDataEnabled", 0, RegistryValueKind.DWord, "Privacidad", "privacy.tailored", "Desactivar experiencias personalizadas", ct);
    public async Task<ActionResult> DisableSettingsRecommendationsAsync(CancellationToken ct = default) => await _registry.SetValueWithBackupAsync(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\UserProfileEngagement", "ScoobeSystemSettingEnabled", 0, RegistryValueKind.DWord, "Privacidad", "privacy.settings-recommendations", "Desactivar recomendaciones Configuracion", ct);
    public async Task<ActionResult> DisableWidgetsAsync(CancellationToken ct = default) => await _registry.SetValueWithBackupAsync(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarDa", 0, RegistryValueKind.DWord, "Privacidad", "privacy.widgets", "Desactivar Widgets", ct);
}
