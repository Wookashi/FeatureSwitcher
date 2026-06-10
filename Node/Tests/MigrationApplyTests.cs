using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Wookashi.FeatureSwitcher.Node.Database;

namespace Node.Database.Tests;

public sealed class MigrationApplyTests
{
    private const string V011LastMigration = "20260520133150_AddSoftDeleteAndUsageTracking";

    [Fact]
    public void Upgrade_FromV011WithData_AppliesCleanly_OnRealSqlite()
    {
        // Reproduces the production scenario: a v0.1.1 database (InitialCreate +
        // AddSoftDeleteAndUsageTracking) holding real rows, then upgraded to the latest schema.
        var dbPath = Path.Combine(Path.GetTempPath(), $"fs_node_upgrade_{Guid.NewGuid():N}.db");
        try
        {
            // 1. Bring the database to the exact v0.1.1 migration state.
            using (var context = CreateContext(dbPath))
            {
                context.GetService<IMigrator>().Migrate(V011LastMigration);
            }

            // 2. Seed data so ApplicationFeatures gets populated during the upgrade.
            using (var context = CreateContext(dbPath))
            {
                context.Database.ExecuteSqlRaw(
                    "INSERT INTO Applications (Name, LastUsedAt, Status) VALUES ('AppA', CURRENT_TIMESTAMP, 0);");
                context.Database.ExecuteSqlRaw(
                    "INSERT INTO Features (Name, IsEnabled, ApplicationId, LastUsedAt, Status) " +
                    "VALUES ('DarkMode', 1, 1, CURRENT_TIMESTAMP, 0);");
            }

            // 3. Upgrade to the latest schema — must not throw.
            using (var context = CreateContext(dbPath))
            {
                context.Database.Migrate();
                Assert.Contains("20260610000000_AddApplicationFeatureLifecycle",
                    context.Database.GetAppliedMigrations());
            }
        }
        finally
        {
            SqliteConnectionPoolCleanup();
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
            }
        }
    }

    [Fact]
    public void AllMigrations_ApplyCleanly_OnRealSqlite()
    {
        // The in-memory provider never executes the generated DDL, so a migration that
        // SQLite rejects (e.g. ADD COLUMN with a non-constant default) only surfaces against
        // a real SQLite database. This guards every migration on the actual provider.
        var dbPath = Path.Combine(Path.GetTempPath(), $"fs_node_migrations_{Guid.NewGuid():N}.db");
        try
        {
            var options = new DbContextOptionsBuilder<FeaturesDataContext>()
                .UseSqlite($"Data Source={dbPath}")
                .Options;

            using var context = new FeaturesDataContext(options);

            context.Database.Migrate();

            Assert.NotEmpty(context.Database.GetAppliedMigrations());
        }
        finally
        {
            SqliteConnectionPoolCleanup();
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
            }
        }
    }

    private static FeaturesDataContext CreateContext(string dbPath)
    {
        var options = new DbContextOptionsBuilder<FeaturesDataContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;
        return new FeaturesDataContext(options);
    }

    // SQLite keeps file handles in a connection pool; clear it so the temp file can be deleted.
    private static void SqliteConnectionPoolCleanup() =>
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
}
