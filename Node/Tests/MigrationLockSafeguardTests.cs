using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Wookashi.FeatureSwitcher.Node.Database;
using Wookashi.FeatureSwitcher.Node.Database.Extensions;

namespace Node.Database.Tests;

public sealed class MigrationLockSafeguardTests
{
    [Fact]
    public void ClearStaleMigrationLock_RemovesOrphanedLock_WhenOlderThanThreshold()
    {
        using var connection = OpenSharedSqliteConnection();
        using var context = CreateContext(connection);

        CreateLockTable(connection);
        InsertLock(connection, DateTimeOffset.UtcNow.AddMinutes(-10));

        ConfigureServices.ClearStaleMigrationLock(context, NullLogger.Instance);

        Assert.Equal(0, CountLockRows(connection));
    }

    [Fact]
    public void ClearStaleMigrationLock_LeavesLock_WhenRecent()
    {
        using var connection = OpenSharedSqliteConnection();
        using var context = CreateContext(connection);

        CreateLockTable(connection);
        InsertLock(connection, DateTimeOffset.UtcNow.AddSeconds(-5));

        ConfigureServices.ClearStaleMigrationLock(context, NullLogger.Instance);

        Assert.Equal(1, CountLockRows(connection));
    }

    [Fact]
    public void ClearStaleMigrationLock_DoesNothing_WhenLockTableMissing()
    {
        using var connection = OpenSharedSqliteConnection();
        using var context = CreateContext(connection);

        // No __EFMigrationsLock table at all (fresh database) — must not throw.
        ConfigureServices.ClearStaleMigrationLock(context, NullLogger.Instance);

        Assert.False(LockTableExists(connection));
    }

    private static SqliteConnection OpenSharedSqliteConnection()
    {
        // A shared in-memory connection that stays open keeps the schema alive for the context.
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        return connection;
    }

    private static FeaturesDataContext CreateContext(DbConnection connection)
    {
        var options = new DbContextOptionsBuilder<FeaturesDataContext>()
            .UseSqlite(connection)
            .Options;
        return new FeaturesDataContext(options);
    }

    private static void CreateLockTable(DbConnection connection)
    {
        ExecuteNonQuery(
            connection,
            "CREATE TABLE \"__EFMigrationsLock\" (\"Id\" INTEGER NOT NULL CONSTRAINT \"PK___EFMigrationsLock\" PRIMARY KEY, \"Timestamp\" TEXT NOT NULL);");
    }

    private static void InsertLock(DbConnection connection, DateTimeOffset timestamp)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO \"__EFMigrationsLock\"(\"Id\", \"Timestamp\") VALUES(1, $ts);";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "$ts";
        parameter.Value = timestamp.ToString("o");
        command.Parameters.Add(parameter);
        command.ExecuteNonQuery();
    }

    private static long CountLockRows(DbConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM \"__EFMigrationsLock\";";
        return Convert.ToInt64(command.ExecuteScalar());
    }

    private static bool LockTableExists(DbConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = '__EFMigrationsLock';";
        return Convert.ToInt64(command.ExecuteScalar()) > 0;
    }

    private static void ExecuteNonQuery(DbConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }
}
