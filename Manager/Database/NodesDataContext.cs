using Microsoft.EntityFrameworkCore;
using Wookashi.FeatureSwitcher.Manager.Database.Entities;

namespace Wookashi.FeatureSwitcher.Manager.Database;

public class NodesDataContext : DbContext, INodeDataContext
{
    public NodesDataContext(DbContextOptions<NodesDataContext> options)
        : base(options)
    {
    }
    
    public DbSet<NodeEntity> Nodes { get; set; }
}