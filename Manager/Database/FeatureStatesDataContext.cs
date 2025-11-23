using Microsoft.EntityFrameworkCore;
using Wookashi.FeatureSwitcher.Manager.Database.Entities;

namespace Wookashi.FeatureSwitcher.Manager.Database;

public class FeatureStatesDataContext : DbContext
{
    public FeatureStatesDataContext(DbContextOptions<FeatureStatesDataContext> options)
        : base(options)
    {
    }
    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        // builder.Entity<ApplicationEntity>()
        //     .HasIndex(entity => entity.Name)
        //     .IsUnique();
        // builder.Entity<FeatureEntity>()
        //     .HasIndex(entity => entity.Name)
        //     .IsUnique();
    }
        
    public DbSet<ApplicationEntity> Applications { get; set; }
    public DbSet<FeatureEntity> Features { get; set; }
    public DbSet<FeatureStateEntity> FeatureStates { get; set; }
    public DbSet<NodeEntity> Nodes { get; set; }
}