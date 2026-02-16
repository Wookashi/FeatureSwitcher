using Microsoft.EntityFrameworkCore;
using Wookashi.FeatureSwitcher.Manager.Database.Entities;

namespace Wookashi.FeatureSwitcher.Manager.Database;

internal interface INodeDataContext
{
    public DbSet<NodeEntity> Nodes { get; set; }
    public DbSet<UserEntity> Users { get; set; }
    public DbSet<UserNodeAccessEntity> UserNodeAccess { get; set; }
    public DbSet<AuditLogEntity> AuditLogs { get; set; }
    public int SaveChanges();
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
