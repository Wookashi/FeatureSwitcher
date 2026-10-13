using System.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Wookashi.FeatureSwitcher.Node.Api.Configuration;
using Wookashi.FeatureSwitcher.Node.Api.Extensions;

namespace Wookashi.FeatureSwitcher.Node.Api.HealthChecks;

internal sealed class ManagerHealthCheck(
    IOptions<ManagerSettings> options,
    IHttpClientFactory httpClientFactory,
    ILogger<ManagerHealthCheck> logger) : IHealthCheck
{
    private readonly ManagerSettings _options = options.Value;

    /// <summary>
    /// Manager availability as seen by the previous check, shared across check instances. /health is
    /// polled often (e.g. every few seconds by a container healthcheck), so only changes are logged.
    /// </summary>
    private static bool? _lastAvailable;
    private static readonly Stopwatch UnavailableFor = new();
    private static readonly Lock StateLock = new();

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<string, object>
        {
            { "Manager Url", _options.Url ?? string.Empty },
        };
        try
        {
            var client = httpClientFactory.CreateClient("ManagerHealthCheck");
            using var response = await client.GetAsync($"{_options.Url}/health", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                ReportAvailability(available: true, reason: null);
                return HealthCheckResult.Healthy("Connected", data: data);
            }
            data.Add("Http Code", (int)response.StatusCode);
            ReportAvailability(available: false, reason: $"HTTP {(int)response.StatusCode}");
            return HealthCheckResult.Degraded("Service returned error", data: data);
        }
        catch (Exception ex)
        {
            // A cancellation means no answer arrived in time — either our own HttpClient timeout or the
            // /health caller giving up first (its timeout can be shorter than ours).
            var reason = ex is OperationCanceledException
                ? "no response before the request timed out"
                : $"{ex.GetType().Name}: {ex.Message}";
            ReportAvailability(available: false, reason: reason);
            return HealthCheckResult.Degraded($"Exception: {ex.Message}", data: data);
        }
    }

    private void ReportAvailability(bool available, string? reason)
    {
        lock (StateLock)
        {
            if (_lastAvailable == available)
            {
                return;
            }

            if (available)
            {
                if (_lastAvailable == false)
                {
                    logger.LogInformation("Manager at {ManagerUrl} is available again after {Elapsed}",
                        _options.Url, UnavailableFor.Elapsed.TruncateToSeconds());
                }
                UnavailableFor.Reset();
            }
            else
            {
                logger.LogWarning("Manager at {ManagerUrl} is unavailable ({Reason}); further checks are not logged until it recovers",
                    _options.Url, reason);
                UnavailableFor.Restart();
            }

            _lastAvailable = available;
        }
    }
}
