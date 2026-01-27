using Microsoft.Extensions.DependencyInjection;
using Wookashi.FeatureSwitcher.Client.Abstraction;

namespace Wookashi.FeatureSwitcher.Client.Implementation;

/// <summary>
/// Extension method to configure dependencies for checking flags states.
/// </summary>
public static class ConfigureServicesExtensions
{
    /// <summary>
    /// Configures and registers FeatureManager for dependency injection.
    /// </summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <param name="configuration">Basic client configuration with application name, environment, and node address.</param>
    /// <param name="features">List of features to register with the node.</param>
    /// <returns>Collection of service descriptors.</returns>
    public static IServiceCollection AddFeatureFlags(
        this IServiceCollection services,
        IFeatureSwitcherBasicClientConfiguration configuration,
        List<IFeatureStateModel> features)
    {
        services.AddHttpClient();

        services.AddSingleton<IFeatureManager>(serviceProvider =>
        {
            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

            var featureManager = new FeatureManagerBuilder(configuration)
                .AddFeatures(features)
                .AddHttpClientFactory(httpClientFactory)
                .BuildAsync()
                .GetAwaiter()
                .GetResult();

            return featureManager;
        });

        return services;
    }
}