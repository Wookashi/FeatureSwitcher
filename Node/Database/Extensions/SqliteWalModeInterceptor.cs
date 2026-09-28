using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Wookashi.FeatureSwitcher.Node.Database.Extensions;

/// <summary>
/// Switches every physical SQLite connection to Write-Ahead Logging. Under WAL, readers never
/// block on a concurrent writer (and vice versa) — only writer-vs-writer contention remains, which
/// is handled separately by <c>SqliteConnectionStringBuilder.DefaultTimeout</c> on the connection
/// string (see <see cref="ConfigureServices"/>). Node's workload is dominated by concurrent
/// flag-state reads with occasional writes (feature updates, the soft-delete sweep), so this removes
/// the vast majority of potential SQLITE_BUSY contention.
///
/// journal_mode is persisted in the database file itself, so setting it on every connection open is
/// redundant after the first time — but cheap, and it keeps the mode correct even if the file was
/// ever recreated without it.
///
/// Deliberately does NOT set <c>PRAGMA busy_timeout</c>: verified by direct testing that
/// Microsoft.Data.Sqlite's <c>SqliteCommand</c> ignores that native setting entirely and instead
/// retries a locked write internally for up to <c>SqliteCommand.CommandTimeout</c> (driven by
/// <c>DefaultTimeout</c> on the connection string) — the PRAGMA would be dead code here.
///
/// Skips read-only connections: EF Core's <c>SqliteDatabaseCreator.Exists()</c> (called by
/// <c>Migrate()</c>) opens a separate <c>Mode=ReadOnly</c> connection that still runs interceptors,
/// and switching an existing rollback-journal database to WAL is a write — it fails there with
/// SQLITE_READONLY (Error 8). The next read-write connection performs the switch instead.
/// </summary>
internal sealed class SqliteWalModeInterceptor : DbConnectionInterceptor
{
    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        EnableWal(connection);
        base.ConnectionOpened(connection, eventData);
    }

    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        await EnableWalAsync(connection, cancellationToken);
        await base.ConnectionOpenedAsync(connection, eventData, cancellationToken);
    }

    private static void EnableWal(DbConnection connection)
    {
        if (IsReadOnly(connection))
        {
            return;
        }

        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA journal_mode='WAL';";
        command.ExecuteNonQuery();
    }

    private static async Task EnableWalAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        if (IsReadOnly(connection))
        {
            return;
        }

        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA journal_mode='WAL';";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static bool IsReadOnly(DbConnection connection) =>
        new SqliteConnectionStringBuilder(connection.ConnectionString).Mode == SqliteOpenMode.ReadOnly;
}
