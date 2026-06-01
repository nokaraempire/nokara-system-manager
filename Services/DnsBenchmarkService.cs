using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using NokaraSystemManager.Models;

namespace NokaraSystemManager.Services;

public sealed class DnsBenchmarkService
{
    private static readonly string[] TestDomains = ["google.com", "microsoft.com", "cloudflare.com", "steampowered.com", "epicgames.com", "twitch.tv", "youtube.com"];

    public async Task<IReadOnlyList<DnsTestResult>> BenchmarkAsync(IEnumerable<DnsProvider> providers, CancellationToken cancellationToken = default)
    {
        var results = await Task.WhenAll(providers.Where(p => !string.IsNullOrWhiteSpace(p.Primary)).Select(p => BenchmarkProviderAsync(p, cancellationToken)));
        var list = results.OrderBy(r => r.Score).ToList();
        var recommended = list.FirstOrDefault(r => r.Successes > 0);
        if (recommended is not null) recommended.IsRecommended = true;
        return list;
    }

    private static async Task<DnsTestResult> BenchmarkProviderAsync(DnsProvider provider, CancellationToken cancellationToken)
    {
        var latencies = new List<double>();
        var failures = 0;
        foreach (var domain in TestDomains)
        {
            foreach (var server in new[] { provider.Primary, provider.Secondary }.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct())
            {
                cancellationToken.ThrowIfCancellationRequested();
                var latency = await QueryAsync(server, domain, 1200, cancellationToken);
                if (latency.HasValue) latencies.Add(latency.Value);
                else failures++;
            }
        }

        var successes = latencies.Count;
        var avg = successes == 0 ? 9999 : latencies.Average();
        var worst = successes == 0 ? 0 : latencies.Max();
        var score = avg + failures * 250 + worst * 0.15;
        return new DnsTestResult
        {
            ProviderName = provider.Name,
            Primary = provider.Primary,
            Secondary = provider.Secondary,
            AverageLatencyMs = Math.Round(avg, 2),
            BestLatencyMs = successes == 0 ? 0 : Math.Round(latencies.Min(), 2),
            WorstLatencyMs = Math.Round(worst, 2),
            Failures = failures,
            Successes = successes,
            Score = Math.Round(score, 2)
        };
    }

    private static async Task<double?> QueryAsync(string server, string domain, int timeoutMs, CancellationToken cancellationToken)
    {
        try
        {
            using var udp = new UdpClient();
            var endpoint = new IPEndPoint(IPAddress.Parse(server), 53);
            var id = (ushort)Random.Shared.Next(1, ushort.MaxValue);
            var query = BuildQuery(id, domain);
            var stopwatch = Stopwatch.StartNew();
            await udp.SendAsync(query, query.Length, endpoint);
            var receiveTask = udp.ReceiveAsync();
            var completed = await Task.WhenAny(receiveTask, Task.Delay(timeoutMs, cancellationToken));
            if (completed != receiveTask) return null;
            stopwatch.Stop();
            var response = receiveTask.Result.Buffer;
            var answerCount = response.Length >= 8 ? (response[6] << 8) | response[7] : 0;
            return response.Length >= 12 && response[0] == query[0] && response[1] == query[1] && answerCount > 0 ? stopwatch.Elapsed.TotalMilliseconds : null;
        }
        catch { return null; }
    }

    private static byte[] BuildQuery(ushort id, string domain)
    {
        using var stream = new MemoryStream();
        WriteUInt16(stream, id); WriteUInt16(stream, 0x0100); WriteUInt16(stream, 1); WriteUInt16(stream, 0); WriteUInt16(stream, 0); WriteUInt16(stream, 0);
        foreach (var label in domain.Split('.'))
        {
            var bytes = System.Text.Encoding.ASCII.GetBytes(label);
            stream.WriteByte((byte)bytes.Length);
            stream.Write(bytes);
        }
        stream.WriteByte(0); WriteUInt16(stream, 1); WriteUInt16(stream, 1);
        return stream.ToArray();
    }

    private static void WriteUInt16(Stream stream, ushort value)
    {
        stream.WriteByte((byte)(value >> 8));
        stream.WriteByte((byte)(value & 0xFF));
    }
}
