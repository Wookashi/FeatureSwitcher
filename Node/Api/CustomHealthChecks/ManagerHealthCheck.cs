using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Wookashi.FeatureSwitcher.Node.Api.CustomHealthChecks;

internal sealed class ManagerHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var isHealthy = true;
        
        // TODO check is configurator available

        if (isHealthy)
        {
            var data = new Dictionary<string, object?>
            {
                //TODO Check connection status
                { "status", "Connected" },
            };
            return Task.FromResult(HealthCheckResult.Healthy("Healthy", data));
        }
        else
        {
            //TODO Set status
            var data = new Dictionary<string, object?>
            {
                //TODO Check connection status
                { "status", "Not Connected" }
            };
            return Task.FromResult(HealthCheckResult.Unhealthy("Unhealthy"));
        }
    }
}
