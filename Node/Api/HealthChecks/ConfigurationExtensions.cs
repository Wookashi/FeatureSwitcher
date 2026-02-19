using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace Wookashi.FeatureSwitcher.Node.Api.HealthChecks;

internal static class ConfigurationExtensions
{
    internal static IServiceCollection AddHealthCheckElements(this IServiceCollection services)
    {
        services.AddHealthChecks()
                .AddCheck<ManagerHealthCheck>("Manager");
        return services;
    }

    public static IEndpointRouteBuilder UseHealthCheck(this IEndpointRouteBuilder endpoints,
        string nodeName, string nodeEnvironment, string nodeAddress)
    {
        var data = new Dictionary<string, object>
        {
            { "environment", nodeEnvironment },
            { "address", nodeAddress },
        };
        endpoints.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json; charset=utf-8";
                var result = new
                {
                    name = nodeName,
                    status = report.Status.ToString(),
                    data = data,
                    checks = report.Entries.Select(entry => new
                    {
                        name = entry.Key,
                        status = entry.Value.Status.ToString(),
                        description = entry.Value.Description,
                        data = entry.Value.Data,
                    }),
                };
                await context.Response.WriteAsJsonAsync(result);
            },
        });
        return endpoints;
    }
}