using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wookashi.FeatureSwitcher.Node.Abstraction.Database.Repositories;
using Wookashi.FeatureSwitcher.Node.Database.Repositories;

namespace Wookashi.FeatureSwitcher.Node.Database.Extensions;

public static class ConfigureServices
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddDatabase(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                services.AddDbContext<FeaturesInMemoryDataContext>(options =>
                    options.UseInMemoryDatabase(databaseName: "Test_db"));
                services.AddScoped<IFeaturesDataContext, FeaturesInMemoryDataContext>();
            }
            else
            {
                services.AddDbContext<FeaturesDataContext>(options =>
                    options.UseSqlite(connectionString));
                services.AddScoped<IFeaturesDataContext, FeaturesDataContext>();
            }

            return services.AddScoped<IFeatureRepository, FeatureRepository>();
        }
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