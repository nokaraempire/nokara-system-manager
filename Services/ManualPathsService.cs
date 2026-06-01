using System.Text.Json;
using NokaraSystemManager.Helpers;
using NokaraSystemManager.Models;

namespace NokaraSystemManager.Services;

public sealed class ManualPathsService
{
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public async Task<IReadOnlyList<ManualGamePath>> LoadAsync()
    {
        await EnsureAsync();
        return JsonSerializer.Deserialize<List<ManualGamePath>>(await File.ReadAllTextAsync(AppPaths.ManualPathsFile), _jsonOptions) ?? [];
    }

    public async Task AddAsync(ManualGamePath path)
    {
        var items = (await LoadAsync()).ToList();
        items.Add(path);
        await SaveAsync(items);
    }

    public async Task SaveAsync(IEnumerable<ManualGamePath> paths)
    {
        AppPaths.EnsureBaseFolders();
        await File.WriteAllTextAsync(AppPaths.ManualPathsFile, JsonSerializer.Serialize(paths, _jsonOptions));
    }

    public async Task EnsureAsync()
    {
        AppPaths.EnsureBaseFolders();
        if (!File.Exists(AppPaths.ManualPathsFile))
        {
            await File.WriteAllTextAsync(AppPaths.ManualPathsFile, "[]");
        }
    }
}
