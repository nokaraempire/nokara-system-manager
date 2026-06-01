using System.Net;
using System.Net.NetworkInformation;
using NokaraSystemManager.Models;

namespace NokaraSystemManager.Services;

public sealed class NetworkService
{
    private readonly CommandRunnerService _commands;
    private readonly BackupService _backup;
    private readonly LoggingService _logging;

    public NetworkService(CommandRunnerService commands, BackupService backup, LoggingService logging)
    {
        _commands = commands;
        _backup = backup;
        _logging = logging;
    }

    public IReadOnlyList<DnsProvider> GetDnsProviders() =>
    [
        new DnsProvider { Name = "Automático / proveedor actual" },
        new DnsProvider { Name = "Cloudflare", Primary = "1.1.1.1", Secondary = "1.0.0.1" },
        new DnsProvider { Name = "Google", Primary = "8.8.8.8", Secondary = "8.8.4.4" },
        new DnsProvider { Name = "Quad9", Primary = "9.9.9.9", Secondary = "149.112.112.112" },
        new DnsProvider { Name = "OpenDNS", Primary = "208.67.222.222", Secondary = "208.67.220.220" }
    ];

    public IReadOnlyList<NetworkAdapterInfo> GetAdapters()
    {
        return NetworkInterface.GetAllNetworkInterfaces()
            .Where(n => n.NetworkInterfaceType != NetworkInterfaceType.Loopback && n.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
            .Select(ToAdapterInfo)
            .OrderByDescending(a => a.IsActive)
            .ThenBy(a => a.Name)
            .ToList();
    }

    public async Task<ActionResult> FlushDnsAsync(CancellationToken cancellationToken = default)
    {
        var result = await _commands.RunAsync("ipconfig.exe", "/flushdns", 30000, cancellationToken);
        return result.Success ? ActionResult.Ok("Flush DNS", "Caché DNS limpiada.", result.CombinedOutput) : ActionResult.Fail("Flush DNS", "No se pudo limpiar la caché DNS.", details: result.CombinedOutput);
    }

    public async Task<ActionResult> ReleaseRenewIpAsync(CancellationToken cancellationToken = default)
    {
        var release = await _commands.RunAsync("ipconfig.exe", "/release", 60000, cancellationToken);
        var renew = await _commands.RunAsync("ipconfig.exe", "/renew", 90000, cancellationToken);
        return ActionResult.Combine("Release/Renew IP",
        [
            release.Success ? ActionResult.Ok("Release IP", "IP liberada.", release.CombinedOutput) : ActionResult.Fail("Release IP", "No se pudo liberar la IP.", details: release.CombinedOutput),
            renew.Success ? ActionResult.Ok("Renew IP", "IP renovada.", renew.CombinedOutput) : ActionResult.Fail("Renew IP", "No se pudo renovar la IP.", details: renew.CombinedOutput)
        ]);
    }

    public async Task<ActionResult> ResetWinsockAsync(CancellationToken cancellationToken = default)
    {
        var result = await _commands.RunAsync("netsh.exe", "winsock reset", 60000, cancellationToken);
        return result.Success ? ActionResult.Ok("Reset Winsock", "Winsock reiniciado. Se recomienda reiniciar Windows.", result.CombinedOutput, requiresRestart: true) : ActionResult.Fail("Reset Winsock", "No se pudo reiniciar Winsock.", details: result.CombinedOutput);
    }

    public async Task<ActionResult> ResetTcpIpAsync(CancellationToken cancellationToken = default)
    {
        var result = await _commands.RunAsync("netsh.exe", "int ip reset", 60000, cancellationToken);
        return result.Success ? ActionResult.Ok("Reset TCP/IP", "TCP/IP reiniciado. Se recomienda reiniciar Windows.", result.CombinedOutput, requiresRestart: true) : ActionResult.Fail("Reset TCP/IP", "No se pudo reiniciar TCP/IP.", details: result.CombinedOutput);
    }

    public async Task<ActionResult> SetDnsManualAsync(NetworkAdapterInfo adapter, DnsProvider provider, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(provider.Primary))
        {
            return await SetDnsAutomaticAsync(adapter, cancellationToken);
        }

        var backup = new DnsBackupItem { AdapterId = adapter.Id, AdapterName = adapter.Name, PreviousDns = adapter.DnsServers.ToList(), NewDns = [provider.Primary, provider.Secondary] };
        await _backup.AddDnsBackupAsync(backup);
        var setPrimary = await _commands.RunAsync("netsh.exe", $"interface ip set dns name=\"{adapter.Name}\" static {provider.Primary} validate=no", 30000, cancellationToken);
        if (!setPrimary.Success) return ActionResult.Fail("Cambiar DNS", "No se pudo configurar DNS primario.", details: setPrimary.CombinedOutput);
        var setSecondary = await _commands.RunAsync("netsh.exe", $"interface ip add dns name=\"{adapter.Name}\" addr={provider.Secondary} index=2 validate=no", 30000, cancellationToken);
        var combined = string.Join(Environment.NewLine, setPrimary.CombinedOutput, setSecondary.CombinedOutput);
        if (setSecondary.Success)
        {
            await _logging.LogInfoAsync($"DNS changed for {adapter.Name}: {provider.Primary}, {provider.Secondary}");
            return ActionResult.Ok("Cambiar DNS", $"DNS cambiado a {provider.Name}.", combined);
        }

        return ActionResult.Fail("Cambiar DNS", "DNS primario aplicado, pero fallo el secundario.", details: combined);
    }

    public async Task<ActionResult> SetDnsAutomaticAsync(NetworkAdapterInfo adapter, CancellationToken cancellationToken = default)
    {
        await _backup.AddDnsBackupAsync(new DnsBackupItem { AdapterId = adapter.Id, AdapterName = adapter.Name, PreviousDns = adapter.DnsServers.ToList(), NewDns = [] });
        var result = await _commands.RunAsync("netsh.exe", $"interface ip set dns name=\"{adapter.Name}\" dhcp", 30000, cancellationToken);
        return result.Success ? ActionResult.Ok("Restaurar DNS automático", "DNS restaurado a automático/DHCP.", result.CombinedOutput) : ActionResult.Fail("Restaurar DNS automático", "No se pudo restaurar DNS automático.", details: result.CombinedOutput);
    }

    public async Task<ActionResult> RestoreDnsBackupAsync(DnsBackupItem backup, CancellationToken cancellationToken = default)
    {
        var adapter = GetAdapters().FirstOrDefault(a => a.Id == backup.AdapterId || a.Name == backup.AdapterName);
        if (adapter is null) return ActionResult.Fail("Revertir DNS", "No se encontro el adaptador original.");
        var result = backup.PreviousDns.Count == 0
            ? await SetDnsAutomaticAsync(adapter, cancellationToken)
            : await SetDnsManualAsync(adapter, new DnsProvider { Name = "DNS anterior", Primary = backup.PreviousDns.ElementAtOrDefault(0) ?? string.Empty, Secondary = backup.PreviousDns.ElementAtOrDefault(1) ?? backup.PreviousDns.ElementAtOrDefault(0) ?? string.Empty }, cancellationToken);
        if (result.Success) await _backup.MarkDnsRestoredAsync(backup.Id);
        return result;
    }

    private static NetworkAdapterInfo ToAdapterInfo(NetworkInterface n)
    {
        var props = n.GetIPProperties();
        var unicast = props.UnicastAddresses.FirstOrDefault(a => a.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
        var gateway = props.GatewayAddresses.FirstOrDefault(g => g.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
        return new NetworkAdapterInfo
        {
            Id = n.Id,
            Name = n.Name,
            Description = n.Description,
            IpAddress = unicast?.Address.ToString() ?? "Sin IPv4",
            Gateway = gateway?.Address.ToString() ?? "Sin gateway",
            DnsServers = props.DnsAddresses.Where(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).Select(a => a.ToString()).ToList(),
            IsActive = n.OperationalStatus == OperationalStatus.Up && unicast is not null && gateway is not null && !IPAddress.IsLoopback(unicast.Address)
        };
    }
}
