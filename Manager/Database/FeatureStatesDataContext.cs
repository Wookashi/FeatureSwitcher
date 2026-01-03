using Microsoft.EntityFrameworkCore;
using Wookashi.FeatureSwitcher.Manager.Database.Entities;

namespace Wookashi.FeatureSwitcher.Manager.Database;

public class FeatureStatesDataContext : DbContext
{
    public FeatureStatesDataContext(DbContextOptions<FeatureStatesDataContext> options)
        : base(options)
    {
    }
    
    public DbSet<NodeEntity> Nodes { get; set; }
    public DbSet<StateChangesHistory> StateChanges { get; set; }
}