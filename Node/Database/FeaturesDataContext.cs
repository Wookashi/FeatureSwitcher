using Microsoft.EntityFrameworkCore;
using Wookashi.FeatureSwitcher.Node.Database.Entities;

namespace Wookashi.FeatureSwitcher.Node.Database;

internal sealed class FeaturesDataContext : DbContext
{
    //TODO Use "normal" db in future
    protected override void OnConfiguring
        (DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseInMemoryDatabase(databaseName: "TestDb");
    }
    public DbSet<FeatureEntity> Features { get; set; }
    public DbSet<ApplicationEntity> Applications { get; set; }
}