using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wookashi.FeatureSwitcher.Manager.Abstraction.Database.Repositories;
using Wookashi.FeatureSwitcher.Manager.Database.Repositories;

namespace Wookashi.FeatureSwitcher.Manager.Database.Extensions;

public static class ConfigureServices
{ 
    public static IServiceCollection AddDatabase(this IServiceCollection services, string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            return services.AddScoped<INodeRepository, NodeInMemoryRepository>();
        }
        
        services.AddDbContext<FeatureStatesDataContext>(options =>
            options.UseSqlite(connectionString));
        return services.AddScoped<INodeRepository, NodeRepository>();
    }
    
    public static IApplicationBuilder MigrateDatabase(this IApplicationBuilder app)
    {
        using (var scope = app.ApplicationServices.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FeatureStatesDataContext>();
            db.Database.Migrate();
        }
        return app;
    }
}