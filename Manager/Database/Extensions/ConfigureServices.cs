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
            services.AddDbContext<NodesInMemoryDataContext>(options =>
                options.UseInMemoryDatabase(databaseName: "Test_db"));
            services.AddScoped<INodeDataContext, NodesInMemoryDataContext>();
        }
        else
        {
            services.AddDbContext<NodesDataContext>(options =>
                options.UseSqlite(connectionString));
            services.AddScoped<INodeDataContext, NodesDataContext>();
        }

        return services.AddScoped<INodeRepository, NodeRepository>();
    }
    
    public static IApplicationBuilder MigrateDatabase(this IApplicationBuilder app)
    {
        using (var scope = app.ApplicationServices.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NodesDataContext>();
            db.Database.Migrate();
        }
        return app;
    }
}