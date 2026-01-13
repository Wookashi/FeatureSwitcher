using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Wookashi.FeatureSwitcher.Node.Database.Entities;

namespace Wookashi.FeatureSwitcher.Node.Database;

public class FeaturesDataContext : DbContext, IFeaturesDataContext
{
    public FeaturesDataContext(DbContextOptions<FeaturesDataContext> options)
        : base(options)
    {
    }
        
    public DbSet<FeatureEntity> Features { get; set; }
    public DbSet<ApplicationEntity> Applications { get; set; }
}