using Microsoft.EntityFrameworkCore;
using Wookashi.FeatureSwitcher.Node.Database.Entities;

namespace Wookashi.FeatureSwitcher.Node.Database;

public interface IFeaturesDataContext
{
    DbSet<FeatureEntity> Features { get; set; }
    DbSet<ApplicationEntity> Applications { get; set; }
    DbSet<ApplicationFeatureEntity> ApplicationFeatures { get; set; }
    DbSet<ApplicationFeatureUsageEntity> ApplicationFeatureUsage { get; set; }

    public int SaveChanges();
}
