using Microsoft.EntityFrameworkCore;
using Wookashi.FeatureSwitcher.Manager.Database.Entities;

namespace Wookashi.FeatureSwitcher.Manager.Database;

internal sealed class NodesInMemoryDataContext : DbContext, INodeDataContext
{
    public NodesInMemoryDataContext(DbContextOptions<NodesInMemoryDataContext> options)
        : base(options)
    {
    }
    
    public DbSet<NodeEntity> Nodes { get; set; }
}