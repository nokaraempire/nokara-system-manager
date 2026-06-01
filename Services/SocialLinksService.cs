using System.Diagnostics;
using System.Text.Json;
using NokaraSystemManager.Helpers;
using NokaraSystemManager.Models;

namespace NokaraSystemManager.Services;

public sealed class SocialLinksService
{
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task<SocialLinks> LoadAsync()
    {
        await EnsureAsync();
        return JsonSerializer.Deserialize<SocialLinks>(await File.ReadAllTextAsync(AppPaths.SocialLinksFile), _jsonOptions) ?? DefaultLinks();
    }

    public async Task EnsureAsync()
    {
        AppPaths.EnsureBaseFolders();
        if (File.Exists(AppPaths.SocialLinksFile))
        {
            return;
        }

        var bundled = Path.Combine(AppContext.BaseDirectory, "Config", "social-links.json");
        if (File.Exists(bundled))
        {
            File.Copy(bundled, AppPaths.SocialLinksFile, overwrite: false);
            return;
        }

        await File.WriteAllTextAsync(AppPaths.SocialLinksFile, JsonSerializer.Serialize(DefaultLinks(), _jsonOptions));
    }

    public static IReadOnlyList<SocialLinkItem> ToItems(SocialLinks links) =>
    [
        new() { Label = "GitHub", Url = links.Github },
        new() { Label = "Issues", Url = links.Issues },
        new() { Label = "Instagram", Url = links.Instagram },
        new() { Label = "TikTok", Url = links.Tiktok },
        new() { Label = "YouTube", Url = links.Youtube },
        new() { Label = "Discord", Url = links.Discord },
        new() { Label = "Website", Url = links.Website }
    ];

    public static void OpenUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }

    private static SocialLinks DefaultLinks() => new()
    {
        Github = "https://github.com/nokaraempire",
        Instagram = "https://instagram.com/nokaraempire",
        Tiktok = "https://tiktok.com/@nokaraempire",
        Youtube = "https://youtube.com/@nokaraempire",
        Discord = "",
        Website = "",
        Issues = "https://github.com/nokaraempire/nokara-system-manager/issues"
    };
}
