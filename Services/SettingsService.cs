using System.Text.Json;
using System.Text.Json.Serialization;
using NokaraSystemManager.Helpers;
using NokaraSystemManager.Models;

namespace NokaraSystemManager.Services;

public sealed class SettingsService
{
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true, Converters = { new JsonStringEnumConverter() } };

    public async Task<AppSettings> LoadAsync()
    {
        AppPaths.EnsureBaseFolders();
        if (!File.Exists(AppPaths.SettingsFile)) return new AppSettings();
        return JsonSerializer.Deserialize<AppSettings>(await File.ReadAllTextAsync(AppPaths.SettingsFile), _jsonOptions) ?? new AppSettings();
    }

    public async Task SaveAsync(AppSettings settings)
    {
        AppPaths.EnsureBaseFolders();
        await File.WriteAllTextAsync(AppPaths.SettingsFile, JsonSerializer.Serialize(settings, _jsonOptions));
    }

    public async Task ResetAsync()
    {
        await SaveAsync(new AppSettings());
    }

    public async Task ExportSettingsAsync(string path)
    {
        var settings = await LoadAsync();
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(settings, _jsonOptions));
    }

    public async Task<AppSettings> ImportSettingsAsync(string path)
    {
        var settings = JsonSerializer.Deserialize<AppSettings>(await File.ReadAllTextAsync(path), _jsonOptions) ?? new AppSettings();
        await SaveAsync(settings);
        return settings;
    }

    public async Task EnsureKnownProcessesAsync()
    {
        AppPaths.EnsureBaseFolders();
        if (File.Exists(AppPaths.KnownProcessesFile)) return;
        var bundled = Path.Combine(AppContext.BaseDirectory, "Config", "known-processes.json");
        if (File.Exists(bundled)) File.Copy(bundled, AppPaths.KnownProcessesFile, overwrite: false);
        else await File.WriteAllTextAsync(AppPaths.KnownProcessesFile, "[]");
    }

    public async Task<IReadOnlyList<KnownProcessDefinition>> LoadKnownProcessesAsync()
    {
        await EnsureKnownProcessesAsync();
        return JsonSerializer.Deserialize<List<KnownProcessDefinition>>(await File.ReadAllTextAsync(AppPaths.KnownProcessesFile), _jsonOptions) ?? [];
    }

    public async Task SaveKnownProcessesAsync(IEnumerable<KnownProcessDefinition> definitions)
    {
        AppPaths.EnsureBaseFolders();
        await File.WriteAllTextAsync(AppPaths.KnownProcessesFile, JsonSerializer.Serialize(definitions, _jsonOptions));
    }
}
