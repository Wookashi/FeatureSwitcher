using Microsoft.EntityFrameworkCore;
using Wookashi.FeatureSwitcher.Manager.Database.Entities;
using Wookashi.FeatureSwitcher.Manager.Database.Extensions;

namespace Wookashi.FeatureSwitcher.Manager.Database;

internal sealed class NodesInMemoryDataContext : DbContext, INodeDataContext
{
    public NodesInMemoryDataContext(DbContextOptions<NodesInMemoryDataContext> options)
        : base(options)
    {
    }

    public DbSet<NodeEntity> Nodes { get; set; }
    public DbSet<UserEntity> Users { get; set; }
    public DbSet<UserNodeAccessEntity> UserNodeAccess { get; set; }
    public DbSet<AuditLogEntity> AuditLogs { get; set; }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.UseUtcDateTimes();
    }
}
