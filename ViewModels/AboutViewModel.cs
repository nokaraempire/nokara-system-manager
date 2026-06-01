using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using NokaraSystemManager.Helpers;
using NokaraSystemManager.Models;
using NokaraSystemManager.Services;

namespace NokaraSystemManager.ViewModels;

public sealed class AboutViewModel : ViewModelBase
{
    private readonly AppServices _services;

    public AboutViewModel(AppServices services)
    {
        _services = services;
        CopySystemInfoCommand = new RelayCommand(CopySystemInfo);
        OpenLogsCommand = new RelayCommand(OpenLogs);
        OpenSocialCommand = new RelayCommand(OpenSocial);
    }

    public string AppName => "Nokara System Manager";
    public string Subtitle => "Gaming, Streaming & Maintenance Toolkit";
    public string Creator => "Nokara Empire";
    public string Developer => "kkshi / Fabián Almada";
    public string Version => Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString(3)
        ?? "1.2.0";
    public string DataPath => AppPaths.AppDataRoot;
    public string SafetyNote => "Esta herramienta no promete FPS mágicos. Aplica mantenimiento, ajustes seguros y perfiles de uso. Los resultados dependen del hardware, Windows, drivers y configuración de cada PC.";
    public ObservableCollection<SocialLinkItem> SocialLinks { get; } = [];
    public ICommand CopySystemInfoCommand { get; }
    public ICommand OpenLogsCommand { get; }
    public ICommand OpenSocialCommand { get; }

    public async Task LoadAsync()
    {
        var links = await _services.SocialLinks.LoadAsync();
        SocialLinks.Clear();
        foreach (var item in SocialLinksService.ToItems(links))
        {
            SocialLinks.Add(item);
        }
    }

    private void CopySystemInfo()
    {
        var text = string.Join(Environment.NewLine,
        [
            $"{AppName} {Version}",
            $"Creado por: {Creator}",
            $"Desarrollado por: {Developer}",
            RuntimeInformation.OSDescription,
            RuntimeInformation.OSArchitecture.ToString(),
            Environment.MachineName,
            Environment.UserName
        ]);
        Clipboard.SetText(text);
    }

    private static void OpenSocial(object? parameter)
    {
        if (parameter is SocialLinkItem item && item.IsEnabled)
        {
            SocialLinksService.OpenUrl(item.Url);
        }
    }

    private static void OpenLogs()
    {
        AppPaths.EnsureBaseFolders();
        Process.Start(new ProcessStartInfo("explorer.exe", AppPaths.Logs) { UseShellExecute = true });
    }
}
