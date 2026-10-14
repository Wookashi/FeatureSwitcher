using Microsoft.EntityFrameworkCore;
using Wookashi.FeatureSwitcher.Node.Database.Entities;
using Wookashi.FeatureSwitcher.Node.Database.Extensions;

namespace Wookashi.FeatureSwitcher.Node.Database;

public class FeaturesDataContext : DbContext, IFeaturesDataContext
{
    public FeaturesDataContext(DbContextOptions<FeaturesDataContext> options)
        : base(options)
    {
    }

    public DbSet<FeatureEntity> Features { get; set; }
    public DbSet<ApplicationEntity> Applications { get; set; }
    public DbSet<ApplicationFeatureEntity> ApplicationFeatures { get; set; }
    public DbSet<ApplicationFeatureUsageEntity> ApplicationFeatureUsage { get; set; }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.UseUtcDateTimes();
    }
}
