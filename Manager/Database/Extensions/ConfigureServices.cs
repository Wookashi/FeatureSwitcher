using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wookashi.FeatureSwitcher.Manager.Abstraction.Database.Repositories;
using Wookashi.FeatureSwitcher.Manager.Database.Repositories;
// ReSharper disable UnusedMethodReturnValue.Global

namespace Wookashi.FeatureSwitcher.Manager.Database.Extensions;

public static class ConfigureServices
{
    public static IServiceCollection AddDatabase(this IServiceCollection services, string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            // EF Core shares in-memory databases with the same name across the whole process, so a
            // fixed name let parallel tests see each other's rows. One name per service collection
            // keeps every container isolated while all contexts within it still share one database.
            var databaseName = $"Test_db_{Guid.NewGuid():N}";
            services.AddDbContext<NodesInMemoryDataContext>(options =>
                options.UseInMemoryDatabase(databaseName));
            services.AddScoped<INodeDataContext, NodesInMemoryDataContext>();
        }
        else
        {
            services.AddDbContext<NodesDataContext>(options =>
                options.UseSqlite(connectionString));
            services.AddScoped<INodeDataContext, NodesDataContext>();
        }

        services.AddScoped<INodeRepository, NodeRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();

        return services;
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
