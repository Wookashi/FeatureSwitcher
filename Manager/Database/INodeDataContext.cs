using Microsoft.EntityFrameworkCore;
using Wookashi.FeatureSwitcher.Manager.Database.Entities;

namespace Wookashi.FeatureSwitcher.Manager.Database;

internal interface INodeDataContext
{
    public DbSet<NodeEntity> Nodes { get; set; }
    public int SaveChanges();
}