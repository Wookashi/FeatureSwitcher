using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Wookashi.FeatureSwitcher.Node.Database.Entities;

namespace Wookashi.FeatureSwitcher.Node.Database;

public class FeaturesInMemoryDataContext : DbContext, IFeaturesDataContext
{
    public FeaturesInMemoryDataContext(DbContextOptions<FeaturesInMemoryDataContext> options)
        : base(options)
    {
    }
        
    public DbSet<FeatureEntity> Features { get; set; }
    public DbSet<ApplicationEntity> Applications { get; set; }
}