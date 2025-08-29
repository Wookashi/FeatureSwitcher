using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Wookashi.FeatureSwitcher.Node.Core.Extensions;

public static partial class DependencyInjectionExtensions
{

    /// <summary>
    /// Add a new provider to the dependency injection container. The provider may
    /// provide an implementation of the service, or it may return null.
    /// </summary>
    /// <typeparam name="TService">The service that may be provided.</typeparam>
    /// <param name="services">The dependency injection container.</param>
    /// <param name="func">A handler that provides the service, or null.</param>
    /// <returns>The dependency injection container.</returns>
    public static IServiceCollection AddProvider<TService>(
        this IServiceCollection services,
        Func<IServiceProvider, IConfiguration, TService> func)
        where TService : class
    {
        services.AddSingleton<IProvider<TService>>(new DelegateProvider<TService>(func));

        return services;
    }


    public static IServiceCollection AddDbContextProvider<TContext>(
        this IServiceCollection services,
        string databaseType)
        where TContext : DbContext, IContext
    {
        services.TryAddScoped<IContext>(provider => provider.GetRequiredService<TContext>());
        services.TryAddTransient<IPackageDatabase>(provider => provider.GetRequiredService<PackageDatabase>());

        services.AddDbContext<TContext>();

        services.AddProvider<IContext>((provider, config) =>
        {
            if (!config.HasDatabaseType(databaseType)) return null;

            return provider.GetRequiredService<TContext>();
        });

        services.AddProvider<IPackageDatabase>((provider, config) =>
        {
            if (!config.HasDatabaseType(databaseType)) return null;

            return provider.GetRequiredService<PackageDatabase>();
        });

        services.AddProvider<ISearchIndexer>((provider, config) =>
        {
            if (!config.HasSearchType(DatabaseSearchType)) return null;
            if (!config.HasDatabaseType(databaseType)) return null;

            return provider.GetRequiredService<NullSearchIndexer>();
        });

        services.AddProvider<ISearchService>((provider, config) =>
        {
            if (!config.HasSearchType(DatabaseSearchType)) return null;
            if (!config.HasDatabaseType(databaseType)) return null;

            return provider.GetRequiredService<DatabaseSearchService>();
        });

        services.AddHealthChecks()
            .AddDbContextCheck<TContext>(databaseType, tags: [databaseType]);

        return services;
    }

    /// <summary>
    /// Runs through all providers to resolve the <typeparamref name="TService"/>.
    /// </summary>
    /// <typeparam name="TService">The service that will be resolved using providers.</typeparam>
    /// <param name="services">The dependency injection container.</param>
    /// <returns>An instance of the service created by the providers.</returns>
    public static TService GetServiceFromProviders<TService>(IServiceProvider services)
        where TService : class
    {
        // Run through all the providers for the type. Find the first provider that returns a non-null result.
        var providers = services.GetRequiredService<IEnumerable<IProvider<TService>>>();
        var configuration = services.GetRequiredService<IConfiguration>();

        foreach (var provider in providers)
        {
            var result = provider.GetOrNull(services, configuration);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }
}
