using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Wookashi.FeatureSwitcher.Manager.Database
{
    // ReSharper disable once UnusedType.Global
    public class FeatureStatesDataContextFactory : IDesignTimeDbContextFactory<FeatureStatesDataContext>
    {
        public FeatureStatesDataContext CreateDbContext(string[] args)
        {
            var apiProjectPath = Path.Combine(Directory.GetCurrentDirectory());

            var configuration = new ConfigurationBuilder()
                .SetBasePath(apiProjectPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = configuration["ManagerConfiguration:ConnectionString"] 
                ?? "Data Source=Data/featureSwitcher.db"; // fallback value used when cs is empty

            var optionsBuilder = new DbContextOptionsBuilder<FeatureStatesDataContext>();
            optionsBuilder.UseSqlite(connectionString);

            return new FeatureStatesDataContext(optionsBuilder.Options);
        }
    }
}
