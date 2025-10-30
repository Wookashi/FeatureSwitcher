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
        try
        {
            using var client = new HttpClient();
            var response = await client.GetAsync(_options.Url, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return HealthCheckResult.Healthy("Connected");
            }
            else
            {
                var data = new Dictionary<string, object?>
                {
                    { "Http Code", (int)response.StatusCode }
                };
                return HealthCheckResult.Degraded("Service returned error", data: data);
            }
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"Exception: {ex.Message}");
        }
    }
}
