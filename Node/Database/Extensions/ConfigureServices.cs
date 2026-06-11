using System.Data;
using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Wookashi.FeatureSwitcher.Node.Abstraction.Database.Repositories;
using Wookashi.FeatureSwitcher.Node.Database.Repositories;

namespace Wookashi.FeatureSwitcher.Node.Database.Extensions;

public static class ConfigureServices
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddDatabase(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                services.AddDbContext<FeaturesInMemoryDataContext>(options =>
                    options.UseInMemoryDatabase(databaseName: "Test_db"));
                services.AddScoped<IFeaturesDataContext, FeaturesInMemoryDataContext>();
            }
            else
            {
                services.AddDbContext<FeaturesDataContext>(options =>
                    options.UseSqlite(connectionString));
                services.AddScoped<IFeaturesDataContext, FeaturesDataContext>();
            }

            return services.AddScoped<IFeatureRepository, FeatureRepository>();
        }
    }

    /// <summary>
    /// How old a row in <c>__EFMigrationsLock</c> must be before we treat it as orphaned.
    /// A real migration on this database completes in well under a second, so anything this
    /// old means a previous process died before releasing the lock.
    /// </summary>
    private static readonly TimeSpan StaleMigrationLockThreshold = TimeSpan.FromMinutes(5);

    public static IApplicationBuilder MigrateDatabase(this IApplicationBuilder app, ILogger logger)
    {
        logger.LogInformation("Starting db Migrations");
        using (var scope = app.ApplicationServices.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FeaturesDataContext>();
            ClearStaleMigrationLock(db, logger);
            db.Database.Migrate();
        }
        logger.LogInformation("Db migrations completed");
        return app;
    }

    /// <summary>
    /// EF Core acquires an exclusive lock by inserting a single row into <c>__EFMigrationsLock</c>
    /// and deletes it once migrations finish. If the process is killed mid-migration (OOM, hard
    /// restart, copied-while-running db file) the row is left behind, and every subsequent start
    /// retries <c>INSERT OR IGNORE</c> forever without ever acquiring the lock. This removes such an
    /// orphaned row when it is clearly stale, while leaving a fresh one alone in case a concurrent
    /// migration is genuinely in progress.
    /// </summary>
    internal static void ClearStaleMigrationLock(DbContext db, ILogger logger)
    {
        var connection = db.Database.GetDbConnection();
        var openedHere = false;
        try
        {
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
                openedHere = true;
            }

            // Fresh databases have no lock table yet — nothing to clean up.
            using (var existsCmd = connection.CreateCommand())
            {
                existsCmd.CommandText =
                    "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = '__EFMigrationsLock';";
                if (Convert.ToInt64(existsCmd.ExecuteScalar(), CultureInfo.InvariantCulture) == 0)
                {
                    return;
                }
            }

            DateTimeOffset lockTimestamp;
            using (var selectCmd = connection.CreateCommand())
            {
                selectCmd.CommandText = "SELECT \"Timestamp\" FROM \"__EFMigrationsLock\" LIMIT 1;";
                var raw = selectCmd.ExecuteScalar();
                if (raw is null or DBNull)
                {
                    return; // No lock currently held.
                }

                if (!DateTimeOffset.TryParse(
                        Convert.ToString(raw, CultureInfo.InvariantCulture),
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                        out lockTimestamp))
                {
                    logger.LogWarning(
                        "Migration lock present but its timestamp '{Raw}' could not be parsed; leaving it untouched.",
                        raw);
                    return;
                }
            }

            var age = DateTimeOffset.UtcNow - lockTimestamp;
            if (age < StaleMigrationLockThreshold)
            {
                logger.LogInformation(
                    "Migration lock present and recent ({Age:g} old); assuming a concurrent migration and leaving it.",
                    age);
                return;
            }

            using var deleteCmd = connection.CreateCommand();
            deleteCmd.CommandText = "DELETE FROM \"__EFMigrationsLock\";";
            var removed = deleteCmd.ExecuteNonQuery();
            logger.LogWarning(
                "Cleared {Count} stale migration lock row(s) ({Age:g} old, threshold {Threshold:g}). " +
                "This usually means a previous migration was interrupted before releasing the lock.",
                removed, age, StaleMigrationLockThreshold);
        }
        catch (Exception ex)
        {
            // The safeguard must never block startup itself — let Migrate() proceed and decide.
            logger.LogWarning(ex, "Failed to check/clear stale migration lock; continuing to Migrate().");
        }
        finally
        {
            if (openedHere && connection.State == ConnectionState.Open)
            {
                connection.Close();
            }
        }
    }
}