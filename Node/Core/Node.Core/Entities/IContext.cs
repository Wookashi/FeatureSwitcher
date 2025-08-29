using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Wookashi.FeatureSwitcher.Node.Core.Entities;

public interface IContext
{
    DatabaseFacade Database { get; }
    
    /// <summary>
    /// Check whether a <see cref="DbUpdateException"/> is due to a SQL unique constraint violation.
    /// </summary>
    /// <param name="exception">The exception to inspect.</param>
    /// <returns>Whether the exception was caused to SQL unique constraint violation.</returns>
    bool IsUniqueConstraintViolationException(DbUpdateException exception);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Applies any pending migrations for the context to the database.
    /// Creates the database if it does not already exist.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the task.</param>
    /// <returns>A task that completes once migrations are applied.</returns>
    Task RunMigrationsAsync(CancellationToken cancellationToken);
}
