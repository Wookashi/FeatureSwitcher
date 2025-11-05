using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Wookashi.FeatureSwitcher.Manager.Database
{
    // ReSharper disable once UnusedType.Global
    public class FeaturesDataContextFactory : IDesignTimeDbContextFactory<FeaturesDataContext>
    {
        public FeaturesDataContext CreateDbContext(string[] args)
        {
            var apiProjectPath = Path.Combine(Directory.GetCurrentDirectory());

            var configuration = new ConfigurationBuilder()
                .SetBasePath(apiProjectPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = configuration["NodeConfiguration:ConnectionString"] 
                ?? "Data Source=featureSwitcher.db"; // fallback gdy brak connection string

            var optionsBuilder = new DbContextOptionsBuilder<FeaturesDataContext>();
            optionsBuilder.UseSqlite(connectionString);

            return new FeaturesDataContext(optionsBuilder.Options);
        }
    }
}
