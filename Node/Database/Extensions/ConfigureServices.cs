using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wookashi.FeatureSwitcher.Node.Abstraction.Database.Repositories;
using Wookashi.FeatureSwitcher.Node.Database.Repositories;

namespace Wookashi.FeatureSwitcher.Node.Database.Extensions;

public static class ConfigureServices
{ 
    public static IServiceCollection AddDatabase(this IServiceCollection services, string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            return services.AddScoped<IFeatureRepository, FeatureInMemoryRepository>();
        }
        
        services.AddDbContext<FeaturesDataContext>(options =>
            options.UseSqlite(connectionString));
        return services.AddScoped<IFeatureRepository, FeatureRepository>();
    }
    
    public static IApplicationBuilder MigrateDatabase(this IApplicationBuilder app)
    {
        using (var scope = app.ApplicationServices.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FeaturesDataContext>();
            db.Database.Migrate();
        }
        return app;
    }
}