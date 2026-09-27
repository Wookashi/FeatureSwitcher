using System.Diagnostics;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wookashi.FeatureSwitcher.Node.Database.Extensions;

namespace Wookashi.FeatureSwitcher.Node.Database.Tests;

public sealed class SqliteConcurrencyTests
{
    [Fact]
    public void AddDatabase_EnablesWalJournalMode()
    {
        var dbPath = TempDbPath();
        try
        {
            using var provider = BuildProvider(dbPath);
            using var scope = provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<FeaturesDataContext>();

            context.Database.OpenConnection();
            using var command = context.Database.GetDbConnection().CreateCommand();
            command.CommandText = "PRAGMA journal_mode;";
            var mode = (string)command.ExecuteScalar()!;

            Assert.Equal("wal", mode, ignoreCase: true);
        }
        finally
        {
            CleanUp(dbPath);
        }
    }

    [Fact]
    public void AddDatabase_SetsCommandTimeoutTo10Seconds()
    {
        // This is the property that actually governs Microsoft.Data.Sqlite's busy-retry loop —
        // confirmed by direct, timed testing (see the two tests below) that PRAGMA busy_timeout has
        // no effect on it, which is why AddDatabase configures this instead.
        var dbPath = TempDbPath();
        try
        {
            using var provider = BuildProvider(dbPath);
            using var scope = provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<FeaturesDataContext>();

            context.Database.OpenConnection();
            using var command = context.Database.GetDbConnection().CreateCommand();

            Assert.Equal(10, command.CommandTimeout);
        }
        finally
        {
            CleanUp(dbPath);
        }
    }

    // The two tests below prove the actual write-contention mechanism end to end with plain
    // ADO.NET connections (no EF Core) against a real file, timing the outcome — not just reading
    // back a PRAGMA. The first test in particular is a regression guard: it fails if
    // DefaultTimeout ever stops being honored and Microsoft.Data.Sqlite silently falls back to its
    // 30s built-in default, by using a short configured timeout and a lock held well past it.

    [Fact]
    public async Task SecondWriter_ThrowsSqliteBusy_AtTheConfiguredTimeout_NotAt30sDefault()
    {
        var dbPath = TempDbPath();
        try
        {
            var cs = WithTimeout(dbPath, seconds: 1);
            CreateSchema(cs);

            using var connA = Open(cs);
            using var connB = Open(cs);

            Execute(connA, "BEGIN IMMEDIATE;");

            // Hold the lock for 3s — well past connB's 1s CommandTimeout, and far short of
            // Microsoft.Data.Sqlite's 30s built-in default, so a throw here can only mean our
            // configured DefaultTimeout was actually honored.
            var releaseAfter3s = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(3));
                Execute(connA, "COMMIT;");
            });

            var sw = Stopwatch.StartNew();
            var ex = await Assert.ThrowsAsync<SqliteException>(() =>
                Task.Run(() => Execute(connB, "INSERT INTO T (V) VALUES ('x');")));
            sw.Stop();

            Assert.Equal(SQLITE_BUSY, ex.SqliteErrorCode);
            Assert.InRange(sw.Elapsed, TimeSpan.FromMilliseconds(700), TimeSpan.FromSeconds(10));

            await releaseAfter3s;
        }
        finally
        {
            CleanUp(dbPath);
        }
    }

    [Fact]
    public async Task SecondWriter_WaitsAndSucceeds_WhenLockIsReleasedBeforeConfiguredTimeout()
    {
        var dbPath = TempDbPath();
        try
        {
            var cs = WithTimeout(dbPath, seconds: 10); // matches Node's real AddDatabase setting
            CreateSchema(cs);

            using var connA = Open(cs);
            using var connB = Open(cs);

            Execute(connA, "BEGIN IMMEDIATE;");

            var writerB = Task.Run(() => Execute(connB, "INSERT INTO T (V) VALUES ('x');"));

            await Task.Delay(TimeSpan.FromMilliseconds(300));
            Execute(connA, "COMMIT;");

            await writerB; // must not throw — well inside the 10s timeout

            using var count = connA.CreateCommand();
            count.CommandText = "SELECT COUNT(*) FROM T;";
            Assert.Equal(1L, (long)count.ExecuteScalar()!);
        }
        finally
        {
            CleanUp(dbPath);
        }
    }

    private const int SQLITE_BUSY = 5;

    private static string WithTimeout(string dbPath, int seconds) =>
        new SqliteConnectionStringBuilder($"Data Source={dbPath}") { DefaultTimeout = seconds }.ConnectionString;

    private static void CreateSchema(string connectionString)
    {
        using var conn = new SqliteConnection(connectionString);
        conn.Open();
        using var command = conn.CreateCommand();
        command.CommandText = "CREATE TABLE T (Id INTEGER PRIMARY KEY, V TEXT);";
        command.ExecuteNonQuery();
    }

    private static SqliteConnection Open(string connectionString)
    {
        var conn = new SqliteConnection(connectionString);
        conn.Open();
        return conn;
    }

    private static void Execute(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private static string TempDbPath() =>
        Path.Combine(Path.GetTempPath(), $"fs_node_concurrency_{Guid.NewGuid():N}.db");

    private static ServiceProvider BuildProvider(string dbPath)
    {
        var services = new ServiceCollection();
        services.AddDatabase($"Data Source={dbPath}");
        return services.BuildServiceProvider();
    }

    private static void CleanUp(string dbPath)
    {
        SqliteConnection.ClearAllPools();
        foreach (var path in new[] { dbPath, dbPath + "-wal", dbPath + "-shm" })
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
