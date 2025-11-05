using Microsoft.EntityFrameworkCore;
using Wookashi.FeatureSwitcher.Manager.Database.Entities;

namespace Wookashi.FeatureSwitcher.Manager.Database;

internal sealed class FeaturesInMemoryDataContext : DbContext
{
    protected override void OnConfiguring
        (DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseInMemoryDatabase(databaseName: "TestDb");
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