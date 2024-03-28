using Microsoft.Extensions.DependencyInjection;
using Wookashi.FeatureSwitcher.Node.Abstraction.Database.Repositories;
using Wookashi.FeatureSwitcher.Node.Database.Repositories;

namespace Wookashi.FeatureSwitcher.Node.Database.Extensions;

public static class ConfigureServices
{ 
    public static IServiceCollection AddDatabase(this IServiceCollection services)
            => services.AddScoped<IFeatureRepository, FeatureRepository>();
}