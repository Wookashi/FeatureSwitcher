using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wookashi.FeatureSwitcher.Manager.Abstraction.Database.Repositories;
using Wookashi.FeatureSwitcher.Manager.Database.Entities;
using Wookashi.FeatureSwitcher.Manager.Database.Extensions;

namespace Wookashi.FeatureSwitcher.Manager.Database.Tests;

public sealed class AuditLogRetentionTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"fs_manager_audit_{Guid.NewGuid():N}.db");
    private ServiceProvider? _provider;

    [Theory]
    [InlineData(true)]  // SQLite file database
    [InlineData(false)] // in-memory database
    public void DeleteEntriesOlderThan_DeletesOnlyEntriesBeforeThreshold(bool useSqlite)
    {
        var now = DateTime.UtcNow;
        var action = NewAction();
        Seed(useSqlite, action, now.AddDays(-200), now.AddDays(-91), now.AddDays(-89), now.AddMinutes(-1));

        using var scope = _provider!.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IAuditLogRepository>();

        var deleted = repository.DeleteEntriesOlderThan(now.AddDays(-90), batchSize: 100);

        Assert.Equal(2, deleted);
        var remaining = repository.GetRecentEntries(100, 0, action);
        Assert.Equal(2, remaining.Count);
        Assert.All(remaining, e => Assert.True(e.Timestamp >= now.AddDays(-90)));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DeleteEntriesOlderThan_DeletesOldestFirstAndRespectsBatchSize(bool useSqlite)
    {
        var now = DateTime.UtcNow;
        var action = NewAction();
        Seed(useSqlite, action, now.AddDays(-300), now.AddDays(-200), now.AddDays(-100), now);

        using var scope = _provider!.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IAuditLogRepository>();
        var threshold = now.AddDays(-90);

        Assert.Equal(2, repository.DeleteEntriesOlderThan(threshold, batchSize: 2));
        var afterFirstBatch = repository.GetRecentEntries(100, 0, action);
        Assert.Equal(2, afterFirstBatch.Count);
        Assert.Equal(now.AddDays(-100), afterFirstBatch[^1].Timestamp, TimeSpan.FromMilliseconds(1));

        Assert.Equal(1, repository.DeleteEntriesOlderThan(threshold, batchSize: 2));
        Assert.Equal(0, repository.DeleteEntriesOlderThan(threshold, batchSize: 2));
        Assert.Single(repository.GetRecentEntries(100, 0, action));
    }

    private static string NewAction() => $"Test-{Guid.NewGuid():N}"[..32];

    private void Seed(bool useSqlite, string action, params DateTime[] timestamps)
    {
        var services = new ServiceCollection();
        services.AddDatabase(useSqlite ? $"Data Source={_dbPath}" : string.Empty);
        _provider = services.BuildServiceProvider();

        using var scope = _provider.CreateScope();
        if (useSqlite)
        {
            scope.ServiceProvider.GetRequiredService<NodesDataContext>().Database.Migrate();
        }

        var context = scope.ServiceProvider.GetRequiredService<INodeDataContext>();
        foreach (var timestamp in timestamps)
        {
            context.AuditLogs.Add(new AuditLogEntity
            {
                Username = "test",
                Action = action,
                Timestamp = timestamp,
            });
        }
        context.SaveChanges();
    }

    public void Dispose()
    {
        _provider?.Dispose();
        SqliteConnection.ClearAllPools();
        foreach (var path in new[] { _dbPath, _dbPath + "-wal", _dbPath + "-shm" })
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
