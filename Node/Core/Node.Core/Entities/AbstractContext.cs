using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Wookashi.FeatureSwitcher.Node.Core.Entities;

public abstract class AbstractContext<TContext> : DbContext, IContext where TContext : DbContext
{
    protected AbstractContext(DbContextOptions<TContext> efOptions)
        : base(efOptions)
    { }

    public DbSet<ApplicationEntity> Applications { get; set; }
    public DbSet<FeatureEntity> Features { get; set; }

    public Task<int> SaveChangesAsync() => SaveChangesAsync(default);

    public virtual async Task RunMigrationsAsync(CancellationToken cancellationToken)
        => await Database.MigrateAsync(cancellationToken);
    public abstract bool IsUniqueConstraintViolationException(DbUpdateException exception);
    
}
