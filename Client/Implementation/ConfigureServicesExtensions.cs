using Microsoft.Extensions.DependencyInjection;
using Wookashi.FeatureSwitcher.Client.Abstraction;

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
    /// <param name="applicationName">Unique name for your application.</param>
    /// <param name="environmentName">Environment name (e.g., "Development", "Production").</param>
    /// <param name="nodeAddress">URI of the Feature Switcher Node service.</param>
    /// <param name="features">List of features to register.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// services.AddFeatureFlags(
    ///     applicationName: "MyApp",
    ///     environmentName: "Production",
    ///     nodeAddress: new Uri("http://localhost:8081/"),
    ///     features: new List&lt;FeatureStateModel&gt;
    ///     {
    ///         new("DarkMode", initialState: false),
    ///         new("NewCheckout", initialState: true),
    ///     });
    /// </example>
    public static IServiceCollection AddFeatureFlags(
        this IServiceCollection services,
        string applicationName,
        string environmentName,
        Uri nodeAddress,
        List<IFeatureStateModel> features)
    {
        services.AddHttpClient();

        services.AddSingleton<IFeatureManager>(serviceProvider =>
        {
            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

            var featureManager = new FeatureManager(
                applicationName,
                environmentName,
                nodeAddress,
                features,
                httpClientFactory);

            // Register features with the node (sync wrapper required for DI factory)
            featureManager.RegisterFeaturesOnNodeAsync().GetAwaiter().GetResult();

            return featureManager;
        });

        return services;
    }

    /// <summary>
    /// Registers FeatureManager with the dependency injection container using a configuration object.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Configuration with application name, environment, and node address.</param>
    /// <param name="features">List of features to register.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddFeatureFlags(
        this IServiceCollection services,
        IFeatureSwitcherBasicClientConfiguration configuration,
        List<IFeatureStateModel> features)
    {
        if (configuration is null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        return services.AddFeatureFlags(
            configuration.ApplicationName,
            configuration.EnvironmentName,
            configuration.NodeAddress,
            features);
    }
}
