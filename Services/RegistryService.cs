using System.Text.Json;
using Microsoft.Win32;
using NokaraSystemManager.Models;

namespace NokaraSystemManager.Services;

public sealed class RegistryService
{
    private readonly BackupService _backup;
    private readonly LoggingService _logging;

    public RegistryService(BackupService backup, LoggingService logging)
    {
        _backup = backup;
        _logging = logging;
    }

    public async Task<ActionResult> SetValueWithBackupAsync(RegistryHive hive, string path, string name, object newValue, RegistryValueKind newKind, string module, string actionId, string actionName, CancellationToken cancellationToken = default)
    {
        try
        {
            var backup = await Task.Run(() =>
            {
                using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Default);
                using var key = baseKey.CreateSubKey(path, writable: true) ?? throw new InvalidOperationException($"No se pudo abrir {hive}\\{path}");
                var existedBefore = key.GetValueNames().Any(n => string.Equals(n, name, StringComparison.OrdinalIgnoreCase));
                var previousValue = existedBefore ? key.GetValue(name) : null;
                var previousKind = existedBefore ? key.GetValueKind(name) : RegistryValueKind.Unknown;
                var item = new RegistryBackupItem
                {
                    Module = module,
                    ActionId = actionId,
                    ActionName = actionName,
                    Hive = hive,
                    Path = path,
                    Name = name,
                    ExistedBefore = existedBefore,
                    PreviousValueKind = previousKind,
                    PreviousValueJson = Serialize(previousValue, previousKind),
                    NewValueKind = newKind,
                    NewValueJson = Serialize(newValue, newKind),
                    Result = "Backup creado"
                };
                key.SetValue(name, newValue, newKind);
                return item;
            }, cancellationToken);
            await _backup.AddRegistryBackupAsync(backup);
            await _logging.LogInfoAsync($"Registry set with backup: {backup.Target}");
            return ActionResult.Ok(actionName, $"Valor actualizado: {backup.Target}");
        }
        catch (Exception ex)
        {
            await _logging.LogErrorAsync($"Registry set failed: {hive}\\{path}\\{name}", ex);
            return ActionResult.Fail(actionName, "No se pudo modificar el registro. Revisa permisos y detalles.", ex);
        }
    }

    public async Task<ActionResult> RestoreAsync(RegistryBackupItem backup, CancellationToken cancellationToken = default)
    {
        if (!backup.CanRestore || backup.IsRestored)
        {
            return ActionResult.Fail("Revertir registro", "Este backup no está disponible para revertir.");
        }

        try
        {
            await Task.Run(() =>
            {
                using var baseKey = RegistryKey.OpenBaseKey(backup.Hive, RegistryView.Default);
                using var key = baseKey.CreateSubKey(backup.Path, writable: true) ?? throw new InvalidOperationException($"No se pudo abrir {backup.Hive}\\{backup.Path}");
                if (!backup.ExistedBefore)
                {
                    key.DeleteValue(backup.Name, throwOnMissingValue: false);
                    return;
                }

                key.SetValue(backup.Name, Deserialize(backup.PreviousValueJson, backup.PreviousValueKind) ?? string.Empty, backup.PreviousValueKind);
            }, cancellationToken);
            await _backup.MarkRegistryRestoredAsync(backup.Id, "Restaurado");
            await _logging.LogInfoAsync($"Registry restored: {backup.Target}");
            return ActionResult.Ok("Revertir registro", $"Restaurado: {backup.Target}");
        }
        catch (Exception ex)
        {
            await _logging.LogErrorAsync($"Registry restore failed: {backup.Target}", ex);
            return ActionResult.Fail("Revertir registro", "No se pudo restaurar el valor de registro.", ex);
        }
    }

    private static string? Serialize(object? value, RegistryValueKind kind)
    {
        if (value is null || kind == RegistryValueKind.Unknown) return null;
        var model = new RegistrySerializedValue { Kind = kind };
        switch (kind)
        {
            case RegistryValueKind.DWord: model.DwordValue = Convert.ToInt32(value); break;
            case RegistryValueKind.QWord: model.QwordValue = Convert.ToInt64(value); break;
            case RegistryValueKind.MultiString: model.StringArrayValue = value as string[] ?? []; break;
            case RegistryValueKind.Binary: model.BinaryBase64 = Convert.ToBase64String(value as byte[] ?? []); break;
            default: model.StringValue = value.ToString(); break;
        }
        return JsonSerializer.Serialize(model);
    }

    private static object? Deserialize(string? json, RegistryValueKind kind)
    {
        if (json is null || kind == RegistryValueKind.Unknown) return null;
        var model = JsonSerializer.Deserialize<RegistrySerializedValue>(json);
        return kind switch
        {
            RegistryValueKind.DWord => model?.DwordValue ?? 0,
            RegistryValueKind.QWord => model?.QwordValue ?? 0L,
            RegistryValueKind.MultiString => model?.StringArrayValue ?? [],
            RegistryValueKind.Binary => string.IsNullOrWhiteSpace(model?.BinaryBase64) ? Array.Empty<byte>() : Convert.FromBase64String(model.BinaryBase64),
            _ => model?.StringValue ?? string.Empty
        };
    }
}
