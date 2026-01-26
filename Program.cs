using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace NetChaos
{
    class Program
    {
        static readonly string[] Targets =
        {
            "tim.it", "eolo.it", "google.com", "cloudflare.com",
            "apple.com", "github.com", "stackoverflow.com", "reddit.com",
            // add up to 50+ if that makes you happy
            "facebook.com",    "youtube.com",    "twitter.com",    "instagram.com",    "linkedin.com",
            "bing.com",    "yahoo.com",    "whatsapp.com",    "tiktok.com",
            "paypal.com",    "dropbox.com",    "adobe.com",    "zoom.us",    "salesforce.com",    "openai.com",    "spotify.com",
            "cnn.com",    "bbc.com"
        };

        const int PingIntervalMs = 1000;
        const int HttpsIntervalMs = 5000;
        const int SummaryIntervalMinutes = 1;
        const int PingTimeoutMs = 2000;

        static readonly ConcurrentDictionary<string, SiteStats> Stats = new();

        static async Task Main(string[] args)
        {
            Console.WriteLine("Network torture test started.\n");

            using CancellationTokenSource cts = new();

            Random Rng = new();

            var randomFiveTargets = Targets
                .OrderBy(_ => Rng.Next())
                .Take(5)
                .ToArray();

            if (args.Length > 0)
                randomFiveTargets = args;

            foreach (var site in randomFiveTargets)
                Stats[site] = new SiteStats();

            var pingTasks = randomFiveTargets.Select(site => Task.Run(() => PingLoop(site, cts.Token)));
            var httpTasks = randomFiveTargets.Select(site => Task.Run(() => HttpLoop(site, cts.Token)));

            var summaryTask = Task.Run(() => SummaryLoop(cts.Token));

            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };

            await Task.WhenAll(pingTasks.Concat(httpTasks).Append(summaryTask));
        }

        static async Task PingLoop(string host, CancellationToken token)
        {
            using Ping ping = new();

            while (!token.IsCancellationRequested)
            {
                try
                {
                    var reply = await ping.SendPingAsync(host, PingTimeoutMs);
                    Stats[host].RegisterPing(reply.Status == IPStatus.Success ? reply.RoundtripTime : (long?)null);
                }
                catch
                {
                    Stats[host].RegisterPing(null);
                }

                await Task.Delay(PingIntervalMs, token);
            }
        }

        static async Task HttpLoop(string host, CancellationToken token)
        {
            using HttpClient client = new()
            {
                Timeout = TimeSpan.FromSeconds(10)
            };

            var url = $"https://{host}";

            while (!token.IsCancellationRequested)
            {
                try
                {
                    var sw = Stopwatch.StartNew();
                    using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, token);
                    sw.Stop();
                    Stats[host].RegisterTtfb(sw.ElapsedMilliseconds);
                }
                catch
                {
                    // website having a bad day, like all of us
                }

                await Task.Delay(HttpsIntervalMs, token);
            }
        }

        static async Task SummaryLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMinutes(SummaryIntervalMinutes), token);

                Console.Write($"\n{DateTime.Now}\n");

                foreach (var kvp in Stats)
                {
                    var s = kvp.Value;
                    var summary = s.SummarizeAndReset();

                    Console.Write($"{kvp.Key.PadLeft(25,' ')}\t");
                    Console.Write($"Ping ms: min={summary.Min.ToString().PadRight(6, ' ')} avg={summary.Avg.ToString("F1").PadRight(6, ' ')} max={summary.Max.ToString().PadRight(6, ' ')}");
                    Console.Write($"Packet loss: {summary.PacketLoss:F2}%\t");
                    Console.Write($"Jitter: {summary.Jitter:F2} ms\t");
                    Console.Write($"TTFB avg: {summary.TtfbAvg:F1} ms\n");
                }
            }
        }
    }

    class SiteStats
    {
        private readonly object _lock = new();
        private readonly List<long> _pings = new();
        private readonly List<long> _ttfb = new();
        private int _sent;
        private int _lost;

        public void RegisterPing(long? ms)
        {
            lock (_lock)
            {
                _sent++;
                if (ms.HasValue)
                    _pings.Add(ms.Value);
                else
                    _lost++;
            }
        }

        public void RegisterTtfb(long ms)
        {
            lock (_lock)
            {
                _ttfb.Add(ms);
            }
        }

        public Summary SummarizeAndReset()
        {
            lock (_lock)
            {
                var ordered = _pings.OrderBy(x => x).ToList();
                double jitter = ordered.Count > 1
                    ? ordered.Zip(ordered.Skip(1), (a, b) => Math.Abs(b - a)).Average()
                    : 0;

                var result = new Summary
                {
                    Min = ordered.FirstOrDefault(),
                    Max = ordered.LastOrDefault(),
                    Avg = ordered.Any() ? ordered.Average() : 0,
                    PacketLoss = _sent > 0 ? (_lost * 100.0 / _sent) : 0,
                    Jitter = jitter,
                    TtfbAvg = _ttfb.Any() ? _ttfb.Average() : 0
                };

                _pings.Clear();
                _ttfb.Clear();
                _sent = 0;
                _lost = 0;

                return result;
            }
        }
    }

    record Summary
    {
        public double Min { get; init; }
        public double Avg { get; init; }
        public double Max { get; init; }
        public double PacketLoss { get; init; }
        public double Jitter { get; init; }
        public double TtfbAvg { get; init; }
    }
}
