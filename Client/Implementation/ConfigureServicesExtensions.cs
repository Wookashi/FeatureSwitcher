using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Wookashi.FeatureSwitcher.Client.Abstraction;
using Wookashi.FeatureSwitcher.Client.Abstraction.Models;

namespace Wookashi.FeatureSwitcher.Client.Implementation;

/// <summary>
/// Extension methods to register Feature Switcher with dependency injection.
/// </summary>
public static class ConfigureServicesExtensions
{
    /// <summary>
    /// Registers FeatureManager with the dependency injection container.
    /// Features are registered with the node during service activation.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">
    /// Configuration with application name, environment, node address, and startup behavior.
    /// <see cref="IFeatureSwitcherBasicClientConfiguration.AllowStartWithoutNode"/> controls whether
    /// the host is allowed to start when the node is unreachable during registration.
    /// </param>
    /// <param name="features">List of features to register.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// services.AddFeatureFlags(
    ///     new FeatureSwitcherBasicClientConfiguration(
    ///         applicationName: "MyApp",
    ///         environmentName: "Production",
    ///         nodeAddress: new Uri("http://localhost:8081/")),
    ///     features: new List&lt;IFeatureStateModel&gt;
    ///     {
    ///         new FeatureStateModel("DarkMode", initialState: false),
    ///         new FeatureStateModel("NewCheckout", initialState: true),
    ///     });
    /// </example>
    public static IServiceCollection AddFeatureFlags(
        this IServiceCollection services,
        IFeatureSwitcherBasicClientConfiguration configuration,
        List<IFeatureStateModel> features)
    {
        if (configuration is null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        services.AddHttpClient();

        services.AddSingleton<FeatureManager>(serviceProvider =>
        {
            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            var logger = serviceProvider.GetService<ILogger<FeatureManager>>();

            return new FeatureManager(
                configuration.ApplicationName,
                configuration.EnvironmentName,
                configuration.NodeAddress,
                features,
                httpClientFactory,
                logger);
        });

        services.AddSingleton<IFeatureManager>(sp => sp.GetRequiredService<FeatureManager>());
        services.AddSingleton<IHostedService>(sp => new FeatureSwitcherStartupService(
            sp.GetRequiredService<FeatureManager>(),
            configuration.AllowStartWithoutNode,
            sp.GetService<ILogger<FeatureSwitcherStartupService>>()));

        return services;
    }
}
