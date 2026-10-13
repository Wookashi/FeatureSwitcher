using Microsoft.Data.Sqlite;

namespace Wookashi.FeatureSwitcher.Node.Database.Extensions;

public static class DatabaseExceptionExtensions
{
    private const int SqliteBusy = 5;
    private const int SqliteLocked = 6;

    /// <summary>
    /// True when the exception (or one of its inner exceptions, e.g. inside EF Core's
    /// DbUpdateException) means the SQLite database was locked by another writer for longer than the
    /// configured timeout — a transient condition, not a bug.
    /// </summary>
    public static bool IsDatabaseBusy(this Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is SqliteException { SqliteErrorCode: SqliteBusy or SqliteLocked })
            {
                return true;
            }
        }

        return false;
    }
}
