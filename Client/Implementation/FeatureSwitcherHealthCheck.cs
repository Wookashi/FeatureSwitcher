using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Wookashi.FeatureSwitcher.Client.Implementation;

/// <summary>
/// Reports whether feature checks get current states from the Node, or fall back to last known or initial
/// states. Add it with <see cref="FeatureSwitcherHealthChecksBuilderExtensions.AddFeatureSwitcher"/>.
/// </summary>
/// <remarks>
/// The check sends no request: it reports the outcome of the latest registration and feature check of
/// this application instance (<see cref="FeatureManager.IsRegistered"/>,
/// <see cref="FeatureManager.IsNodeReachable"/>).
/// </remarks>
public sealed class FeatureSwitcherHealthCheck : IHealthCheck
{
    private readonly FeatureManager _featureManager;

    /// <summary>
    /// Creates the health check for the given feature manager.
    /// </summary>
    /// <param name="featureManager">The application's feature manager.</param>
    public FeatureSwitcherHealthCheck(FeatureManager featureManager)
    {
        _featureManager = featureManager ?? throw new ArgumentNullException(nameof(featureManager));
    }

    /// <inheritdoc />
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var lastContact = _featureManager.LastNodeContactAt;
        var data = new Dictionary<string, object>
        {
            ["registered"] = _featureManager.IsRegistered,
            ["nodeReachable"] = _featureManager.IsNodeReachable,
            ["lastNodeContactAt"] = lastContact?.ToString("o") ?? "never",
        };
        var failureStatus = context.Registration.FailureStatus;

        if (!_featureManager.IsRegistered)
        {
            return Task.FromResult(new HealthCheckResult(
                failureStatus,
                "Features are not registered on the Node: features new to the Node use their initial states. " +
                "Registration is retried in the background.",
                data: data));
        }

        if (!_featureManager.IsNodeReachable)
        {
            return Task.FromResult(new HealthCheckResult(
                failureStatus,
                $"The Node did not respond to the latest request: features use their last known states " +
                $"(last contact: {lastContact?.ToString("u") ?? "never"}).",
                data: data));
        }

        return Task.FromResult(HealthCheckResult.Healthy("Feature states come from the Node.", data));
    }
}

/// <summary>
/// Registers <see cref="FeatureSwitcherHealthCheck"/>.
/// </summary>
public static class FeatureSwitcherHealthChecksBuilderExtensions
{
    /// <summary>
    /// Adds a health check that reports whether feature states come from the Node. Requires
    /// <c>AddFeatureFlags</c>.
    /// </summary>
    /// <param name="builder">The health checks builder.</param>
    /// <param name="name">Name of the check.</param>
    /// <param name="failureStatus">
    /// Status reported while features fall back to last known or initial states. Defaults to
    /// <see cref="HealthStatus.Degraded"/>: the application keeps working, so it should not be restarted or
    /// taken out of rotation because of it — a restart while the Node is unreachable would reset features
    /// to their initial states.
    /// </param>
    /// <param name="tags">Tags of the check, e.g. to include it only in a readiness endpoint.</param>
    /// <example>
    /// <code>
    /// services.AddHealthChecks().AddFeatureSwitcher();
    /// </code>
    /// </example>
    public static IHealthChecksBuilder AddFeatureSwitcher(
        this IHealthChecksBuilder builder,
        string name = "feature-switcher",
        HealthStatus failureStatus = HealthStatus.Degraded,
        IEnumerable<string>? tags = null)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        return builder.AddCheck<FeatureSwitcherHealthCheck>(name, failureStatus, tags ?? []);
    }
}
