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
    public DbSet<UserEntity> Users { get; set; }
    public DbSet<UserNodeAccessEntity> UserNodeAccess { get; set; }
    public DbSet<AuditLogEntity> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<UserNodeAccessEntity>(entity =>
        {
            entity.HasOne(e => e.User)
                .WithMany(u => u.NodeAccess)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Node)
                .WithMany()
                .HasForeignKey(e => e.NodeId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
