using Microsoft.EntityFrameworkCore;
using Wookashi.FeatureSwitcher.Manager.Database.Entities;

namespace Wookashi.FeatureSwitcher.Manager.Database;

public class FeaturesDataContext : DbContext
{
    public FeaturesDataContext(DbContextOptions<FeaturesDataContext> options)
        : base(options)
    {
    }
    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<ApplicationEntity>()
            .HasIndex(entity => entity.Name)
            .IsUnique();
        builder.Entity<FeatureEntity>()
            .HasIndex(entity => entity.Name)
            .IsUnique();
    }
        
    public DbSet<FeatureEntity> Features { get; set; }
    public DbSet<ApplicationEntity> Applications { get; set; }
}