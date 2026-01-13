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

    public static IEndpointRouteBuilder UseHealthCheck(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json; charset=utf-8";
                var result = new
                {
                    status = report.Status.ToString(),
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