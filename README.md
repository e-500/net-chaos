# NetChaos

NetChaos is a lightweight .NET console tool for continuously stress-testing and monitoring network connectivity against multiple public targets.

It performs:

* ICMP ping tests (latency, packet loss, jitter)
* HTTPS requests (TTFB – Time To First Byte)
* Periodic statistical summaries

Designed for quick ISP diagnostics, instability tracking, and general “is it my network or the world?” investigations.

---

## Features

* Concurrent monitoring of multiple hosts
* Per-host statistics:

  * Minimum / Average / Maximum ping
  * Packet loss %
  * Jitter (average delta between consecutive pings)
  * Average HTTPS TTFB
* Rolling summary output (default: every 1 minute)
* Graceful shutdown via `Ctrl + C`
* Thread-safe stats collection

---

## How It Works

For each selected host:

* **Ping Loop**

  * Runs every 1 second
  * Timeout: 2000 ms
  * Tracks:

    * Latency
    * Packet loss
    * Jitter

* **HTTP Loop**

  * Runs every 5 seconds
  * Sends HTTPS GET request
  * Measures:

    * Time To First Byte (TTFB)

Every minute, NetChaos prints a summary and resets the statistics window.

---

## Default Configuration

```csharp
const int PingIntervalMs = 1000;
const int HttpsIntervalMs = 5000;
const int SummaryIntervalMinutes = 1;
const int PingTimeoutMs = 2000;
```

By default, the program:

* Randomly selects 5 targets from the predefined list
* Or uses hosts passed via command-line arguments

---

## Usage

### Run with random targets

```bash
dotnet run
```

### Run with specific targets

```bash
dotnet run google.com cloudflare.com github.com
```

Any arguments passed will override the random selection.

---

## Example Output

```
Network torture test started.


14/02/2026 12:00:08
               openai.com       Ping ms: min=17     avg=20.7   max=25    Packet loss: 0.00%     Jitter: 0.14 ms TTFB avg: 137.4 ms
              youtube.com       Ping ms: min=0      avg=0.0    max=0     Packet loss: 100.00%   Jitter: 0.00 ms TTFB avg: 208.3 ms // youtube.com ignores ping but respond to https call
                apple.com       Ping ms: min=24     avg=26.8   max=42    Packet loss: 0.00%     Jitter: 0.32 ms TTFB avg: 207.9 ms
        stackoverflow.com       Ping ms: min=17     avg=20.6   max=26    Packet loss: 0.00%     Jitter: 0.16 ms TTFB avg: 66.2 ms
               paypal.com       Ping ms: min=19     avg=22.9   max=27    Packet loss: 0.00%     Jitter: 0.14 ms TTFB avg: 380.3 ms

```

---

## Metrics Explained

### Ping (ms)

Round-trip time for ICMP echo request.

* **Min** – Best observed latency
* **Avg** – Average latency
* **Max** – Worst observed latency

### Packet Loss (%)

Percentage of failed ping attempts.

### Jitter (ms)

Average absolute difference between consecutive ping times.
Indicates connection stability.

### TTFB (Time To First Byte)

Time taken to receive the first byte of an HTTPS response.
Reflects server responsiveness + network latency.

---

## When To Use

* Diagnosing ISP instability
* Detecting intermittent packet loss
* Comparing latency between providers
* Monitoring remote office connectivity
* Stress-testing VPN or failover links
* Proving to someone that “it’s not just slow, it’s broken”

---

## Requirements

* .NET 6+ (or compatible runtime)
* Network access
* ICMP allowed by OS / firewall

---

## Notes

* Some hosts may block ICMP or rate-limit HTTPS requests.
* TTFB includes TLS negotiation overhead.
* This tool is intentionally aggressive. Use responsibly.
* Results reflect your local network path and DNS resolution.

---

## Graceful Shutdown

Press:

```
Ctrl + C
```

The application will cancel all loops and exit cleanly.

---

## License

Use it. Break it. Improve it.

---

