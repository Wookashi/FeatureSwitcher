using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Wookashi.FeatureSwitcher.Node.Abstraction.Database.Dtos;
using Wookashi.FeatureSwitcher.Node.Abstraction.Database.Repositories;
using Wookashi.FeatureSwitcher.Node.Database.Entities;
using Wookashi.FeatureSwitcher.Node.Database.Extensions;

namespace Wookashi.FeatureSwitcher.Node.Database.Tests;

public sealed class UsageCountTests : IDisposable
{
    private const string MigrationBeforePerApplicationUsage = "20261013214537_AddApplicationFeatureDetails";

    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"fs_node_usagecount_{Guid.NewGuid():N}.db");
    private readonly ServiceProvider _provider;

    public UsageCountTests()
    {
        var services = new ServiceCollection();
        services.AddDatabase($"Data Source={_dbPath}");
        _provider = services.BuildServiceProvider();
    }

    [Fact]
    public void Registration_UpdatesLastUsedAt_ButIsNotCountedAsRead()
    {
        Migrate();
        var app = new ApplicationDto("App");
        var before = DateTime.UtcNow;

        WithRepository(r => r.RegisterApplication(app, [new FeatureDto("Flag", true)]));
        WithRepository(r => r.RegisterApplication(app, [new FeatureDto("Flag", true)]));
        WithRepository(r => r.FlushFeatureUsage());

        var listed = WithRepository(r => r.GetFeaturesWithUsageForApplication(app)).Single();
        Assert.Equal(0, listed.UsesLast7Days);
        Assert.True(listed.LastUsedAt >= before);
    }

    [Fact]
    public void SharedFlag_CountsReadsPerApplication()
    {
        Migrate();
        var reader = new ApplicationDto("Reader");
        var idle = new ApplicationDto("Idle");
        WithRepository(r => r.RegisterApplication(reader, [new FeatureDto("Shared", true)]));
        WithRepository(r => r.RegisterApplication(idle, [new FeatureDto("Shared", true)]));

        WithRepository(r =>
        {
            r.RecordFeatureUsage(reader, "Shared");
            r.RecordFeatureUsage(reader, "Shared");
            r.RecordFeatureUsage(reader, "Shared");
            return 0;
        });
        WithRepository(r => r.FlushFeatureUsage());

        Assert.Equal(3, WithRepository(r => r.GetFeaturesWithUsageForApplication(reader)).Single().UsesLast7Days);
        Assert.Equal(0, WithRepository(r => r.GetFeaturesWithUsageForApplication(idle)).Single().UsesLast7Days);
    }

    [Fact]
    public void DeleteUsageOlderThan_DeletesWholeDaysBeforeThreshold()
    {
        Migrate();
        var app = new ApplicationDto("App");
        WithRepository(r => r.RegisterApplication(app, [new FeatureDto("Flag", true)]));
        var today = DateTime.UtcNow.Date;

        WithContext(context =>
        {
            var linkId = context.ApplicationFeatures.Single().Id;
            foreach (var daysAgo in new[] { 40, 31, 30, 29, 0 })
            {
                context.ApplicationFeatureUsage.Add(new ApplicationFeatureUsageEntity
                {
                    ApplicationFeatureId = linkId,
                    UsageDay = today.AddDays(-daysAgo),
                    UseCount = 1,
                });
            }
            context.SaveChanges();
        });

        var deleted = WithRepository(r => r.DeleteUsageOlderThan(DateTime.UtcNow.AddDays(-30)));

        Assert.Equal(2, deleted);
        WithContext(context => Assert.Equal(
            [today.AddDays(-30), today.AddDays(-29), today],
            context.ApplicationFeatureUsage.AsNoTracking().Select(u => u.UsageDay).OrderBy(d => d).ToList()));
    }

    [Fact]
    public void List_ReturnsLastReadDay_AndNullWithoutReads()
    {
        Migrate();
        var app = new ApplicationDto("App");
        WithRepository(r => r.RegisterApplication(app, [new FeatureDto("Read", true), new FeatureDto("NeverRead", true)]));
        var today = DateTime.UtcNow.Date;

        WithContext(context =>
        {
            var linkId = context.ApplicationFeatures.Single(l => l.Feature.Name == "Read").Id;
            foreach (var daysAgo in new[] { 20, 10 })
            {
                context.ApplicationFeatureUsage.Add(new ApplicationFeatureUsageEntity
                {
                    ApplicationFeatureId = linkId,
                    UsageDay = today.AddDays(-daysAgo),
                    UseCount = 3,
                });
            }
            context.SaveChanges();
        });

        var listed = WithRepository(r => r.GetFeaturesWithUsageForApplication(app)).ToDictionary(f => f.Name);

        Assert.Equal(today.AddDays(-10), listed["Read"].LastReadAt);
        Assert.Equal(DateTimeKind.Utc, listed["Read"].LastReadAt!.Value.Kind);
        Assert.Equal(0, listed["Read"].UsesLast7Days); // both days are older than the 7-day window
        Assert.Null(listed["NeverRead"].LastReadAt);
    }

    [Fact]
    public void RegisteredAt_IsSetOnFirstRegistration_AndKeptOnReRegistration()
    {
        Migrate();
        var app = new ApplicationDto("App");
        var before = DateTime.UtcNow;
        WithRepository(r => r.RegisterApplication(app, [new FeatureDto("Flag", true)]));
        var registeredAt = WithRepository(r => r.GetFeaturesWithUsageForApplication(app)).Single().RegisteredAt;

        Thread.Sleep(20);
        WithRepository(r => r.RegisterApplication(app, [new FeatureDto("Flag", true)]));

        Assert.True(registeredAt >= before);
        Assert.Equal(registeredAt, WithRepository(r => r.GetFeaturesWithUsageForApplication(app)).Single().RegisteredAt);
    }

    [Fact]
    public void Migration_SetsCreatedAtOfExistingLinksToUpgradeTime()
    {
        using (var context = CreateContext())
        {
            context.GetService<IMigrator>().Migrate("20261014082528_CountUsagePerApplication");
            context.Database.ExecuteSqlRaw(
                """
                INSERT INTO Applications (Id, Name, LastUsedAt, Status) VALUES (1, 'App', '2026-01-01 00:00:00', 0);
                INSERT INTO Features (Id, Name, IsEnabled, LastUsedAt, Status) VALUES (1, 'Flag', 1, '2026-01-01 00:00:00', 0);
                INSERT INTO ApplicationFeatures (Id, ApplicationId, FeatureId, LastUsedAt, Status) VALUES (1, 1, 1, '2026-01-01 00:00:00', 0);
                """);
        }

        var before = DateTime.UtcNow.AddSeconds(-1);
        using (var context = CreateContext())
        {
            context.Database.Migrate();
        }

        var createdAt = WithRepository(r => r.GetFeaturesWithUsageForApplication(new ApplicationDto("App"))).Single().RegisteredAt;
        Assert.InRange(createdAt, before, DateTime.UtcNow.AddSeconds(1));
    }

    [Fact]
    public void Migration_CopiesCountsOfSingleApplicationFlags_AndDropsSharedOnes()
    {
        using (var context = CreateContext())
        {
            context.GetService<IMigrator>().Migrate(MigrationBeforePerApplicationUsage);
            context.Database.ExecuteSqlRaw(
                """
                INSERT INTO Applications (Id, Name, LastUsedAt, Status) VALUES
                    (1, 'AppA', '2026-10-01 00:00:00', 0), (2, 'AppB', '2026-10-01 00:00:00', 0);
                INSERT INTO Features (Id, Name, IsEnabled, LastUsedAt, Status) VALUES
                    (1, 'OnlyA', 1, '2026-10-01 00:00:00', 0), (2, 'Shared', 1, '2026-10-01 00:00:00', 0);
                INSERT INTO ApplicationFeatures (Id, ApplicationId, FeatureId, LastUsedAt, Status) VALUES
                    (10, 1, 1, '2026-10-01 00:00:00', 0), (11, 1, 2, '2026-10-01 00:00:00', 0), (12, 2, 2, '2026-10-01 00:00:00', 0);
                INSERT INTO FeatureUsage (FeatureId, UsageDay, UseCount) VALUES
                    (1, '2026-10-01 00:00:00', 5), (1, '2026-10-02 00:00:00', 7), (2, '2026-10-01 00:00:00', 9);
                """);
        }

        using (var context = CreateContext())
        {
            context.Database.Migrate();

            var rows = context.ApplicationFeatureUsage.AsNoTracking().OrderBy(u => u.UsageDay).ToList();
            Assert.Equal(2, rows.Count);
            Assert.All(rows, u => Assert.Equal(10, u.ApplicationFeatureId));
            Assert.Equal([5L, 7L], rows.Select(u => u.UseCount));
        }
    }

    [Fact]
    public void Migration_Down_SumsCountsPerFlag()
    {
        Migrate();
        WithRepository(r => r.RegisterApplication(new ApplicationDto("AppA"), [new FeatureDto("Shared", true)]));
        WithRepository(r => r.RegisterApplication(new ApplicationDto("AppB"), [new FeatureDto("Shared", true)]));
        WithRepository(r =>
        {
            r.RecordFeatureUsage(new ApplicationDto("AppA"), "Shared");
            r.RecordFeatureUsage(new ApplicationDto("AppB"), "Shared");
            r.RecordFeatureUsage(new ApplicationDto("AppB"), "Shared");
            return r.FlushFeatureUsage();
        });

        using var context = CreateContext();
        context.GetService<IMigrator>().Migrate(MigrationBeforePerApplicationUsage);

        var total = context.Database.SqlQueryRaw<long>("SELECT SUM(UseCount) AS Value FROM FeatureUsage").Single();
        Assert.Equal(3, total);
    }

    public void Dispose()
    {
        _provider.Dispose();
        SqliteConnection.ClearAllPools();
        foreach (var path in new[] { _dbPath, _dbPath + "-wal", _dbPath + "-shm" })
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    private void Migrate() => WithContext(context => context.Database.Migrate());

    private FeaturesDataContext CreateContext() =>
        new(new DbContextOptionsBuilder<FeaturesDataContext>().UseSqlite($"Data Source={_dbPath}").Options);

    private T WithRepository<T>(Func<IFeatureRepository, T> action)
    {
        using var scope = _provider.CreateScope();
        return action(scope.ServiceProvider.GetRequiredService<IFeatureRepository>());
    }

    private void WithRepository(Action<IFeatureRepository> action)
    {
        using var scope = _provider.CreateScope();
        action(scope.ServiceProvider.GetRequiredService<IFeatureRepository>());
    }

    private void WithContext(Action<FeaturesDataContext> action)
    {
        using var scope = _provider.CreateScope();
        action(scope.ServiceProvider.GetRequiredService<FeaturesDataContext>());
    }
}
