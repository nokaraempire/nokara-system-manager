using Microsoft.Win32;
using NokaraSystemManager.Models;

namespace NokaraSystemManager.Services;

public sealed class GamingOptimizationService
{
    private readonly RegistryService _registry;
    private readonly PowerPlanService _powerPlan;

    public GamingOptimizationService(RegistryService registry, PowerPlanService powerPlan)
    {
        _registry = registry;
        _powerPlan = powerPlan;
    }

    public async Task<ActionResult> EnableGameModeAsync(CancellationToken ct = default)
    {
        return await _registry.SetValueWithBackupAsync(RegistryHive.CurrentUser, @"Software\Microsoft\GameBar", "AutoGameModeEnabled", 1, RegistryValueKind.DWord, "Gaming", "gaming.game-mode", "Activar Game Mode", ct);
    }

    public async Task<ActionResult> DisableGameDvrAsync(CancellationToken ct = default)
    {
        var r1 = await _registry.SetValueWithBackupAsync(RegistryHive.CurrentUser, @"System\GameConfigStore", "GameDVR_Enabled", 0, RegistryValueKind.DWord, "Gaming", "gaming.game-dvr", "Desactivar Game DVR", ct);
        var r2 = await _registry.SetValueWithBackupAsync(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\GameDVR", "AppCaptureEnabled", 0, RegistryValueKind.DWord, "Gaming", "gaming.game-dvr", "Desactivar capturas", ct);
        return ActionResult.Combine("Desactivar Game DVR / capturas", [r1, r2]);
    }

    public async Task<ActionResult> DisableTransparencyAsync(CancellationToken ct = default)
    {
        return await _registry.SetValueWithBackupAsync(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "EnableTransparency", 0, RegistryValueKind.DWord, "Gaming", "gaming.transparency", "Desactivar transparencias", ct);
    }

    public async Task<ActionResult> ReduceAnimationsAsync(CancellationToken ct = default)
    {
        var r1 = await _registry.SetValueWithBackupAsync(RegistryHive.CurrentUser, @"Control Panel\Desktop", "UserPreferencesMask", new byte[] { 0x90, 0x12, 0x03, 0x80, 0x10, 0x00, 0x00, 0x00 }, RegistryValueKind.Binary, "Gaming", "gaming.animations", "Reducir animaciones", ct);
        var r2 = await _registry.SetValueWithBackupAsync(RegistryHive.CurrentUser, @"Control Panel\Desktop\WindowMetrics", "MinAnimate", "0", RegistryValueKind.String, "Gaming", "gaming.animations", "Reducir animaciones", ct);
        return ActionResult.Combine("Reducir animaciones", [r1, r2]);
    }

    public async Task<ActionResult> DisableMouseAccelerationAsync(CancellationToken ct = default)
    {
        var r1 = await _registry.SetValueWithBackupAsync(RegistryHive.CurrentUser, @"Control Panel\Mouse", "MouseSpeed", "0", RegistryValueKind.String, "Gaming", "gaming.mouse-acceleration", "Desactivar MouseSpeed", ct);
        var r2 = await _registry.SetValueWithBackupAsync(RegistryHive.CurrentUser, @"Control Panel\Mouse", "MouseThreshold1", "0", RegistryValueKind.String, "Gaming", "gaming.mouse-acceleration", "Desactivar MouseThreshold1", ct);
        var r3 = await _registry.SetValueWithBackupAsync(RegistryHive.CurrentUser, @"Control Panel\Mouse", "MouseThreshold2", "0", RegistryValueKind.String, "Gaming", "gaming.mouse-acceleration", "Desactivar MouseThreshold2", ct);
        return ActionResult.Combine("Desactivar Enhanced Pointer Precision", [r1, r2, r3]);
    }

    public async Task<ActionResult> DisablePowerThrottlingAsync(CancellationToken ct = default)
    {
        return await _registry.SetValueWithBackupAsync(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Power\PowerThrottling", "PowerThrottlingOff", 1, RegistryValueKind.DWord, "Gaming", "gaming.power-throttling", "Desactivar Power Throttling", ct);
    }

    public async Task<ActionResult> ActivatePerformancePowerPlanAsync(CancellationToken ct = default) => await _powerPlan.ActivateBestPerformancePlanAsync(ct);

    public Task<ActionResult> DisableUsbSelectiveSuspendAsync(CancellationToken ct = default)
    {
        var result = ActionResult.Fail("USB Selective Suspend", "Pendiente de implementacion segura en V1.");
        result.Details = "No se modifica USB Selective Suspend hasta validar lectura y reversión completa por esquema de energía.";
        return Task.FromResult(result);
    }

    public Task<ActionResult> RestoreUsbSelectiveSuspendAsync(CancellationToken ct = default) => Task.FromResult(ActionResult.Ok("USB Selective Suspend", "No hay backup USB pendiente en V1."));
}
