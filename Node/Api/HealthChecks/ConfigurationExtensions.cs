using System.Reflection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Wookashi.FeatureSwitcher.Node.Api.HealthChecks;

internal static class ConfigurationExtensions
{
    internal static readonly string AppVersion =
        typeof(ConfigurationExtensions).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion?.Split('+')[0] ?? "unknown";

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

        // Full report including dependencies (Manager). Used by the Manager UI to show node status/version.
        endpoints.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = (context, report) => WriteReport(context, report, nodeName, data),
        });

        // Liveness: answers as long as the node process serves HTTP, without contacting any dependency.
        // Intended for container/orchestrator probes, so that an unavailable or slow Manager can never
        // make a working node look unhealthy (the Manager check alone may take longer than a probe timeout).
        endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = (context, report) => WriteReport(context, report, nodeName, data),
        });

        return endpoints;
    }

    private static Task WriteReport(HttpContext context, HealthReport report, string nodeName,
        Dictionary<string, object> data)
    {
        context.Response.ContentType = "application/json; charset=utf-8";
        var result = new
        {
            name = nodeName,
            version = AppVersion,
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
        return context.Response.WriteAsJsonAsync(result);
    }
}