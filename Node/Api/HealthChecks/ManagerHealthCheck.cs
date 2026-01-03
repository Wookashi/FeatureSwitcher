using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Wookashi.FeatureSwitcher.Node.Api.Configuration;

namespace Wookashi.FeatureSwitcher.Node.Api.HealthChecks;

internal sealed class ManagerHealthCheck(IOptions<ManagerSettings> options) : IHealthCheck
{
    private readonly ManagerSettings _options = options.Value;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<string, object>
        {
            { "Manager Url", _options.Url ?? string.Empty }
        };
        try
        {
            using var client = new HttpClient();
            var response = await client.GetAsync($"{_options.Url}/health", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return HealthCheckResult.Healthy("Connected");
            }
            data.Add("Http Code", (int)response.StatusCode);
            return HealthCheckResult.Degraded("Service returned error", data: data);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Degraded($"Exception: {ex.Message}", data: data);
        }
    }
}
