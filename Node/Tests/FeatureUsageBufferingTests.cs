using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wookashi.FeatureSwitcher.Node.Abstraction.Database.Dtos;
using Wookashi.FeatureSwitcher.Node.Abstraction.Database.Repositories;
using Wookashi.FeatureSwitcher.Node.Database.Entities;
using Wookashi.FeatureSwitcher.Node.Database.Extensions;
using Wookashi.FeatureSwitcher.Node.Database.Repositories;

namespace Wookashi.FeatureSwitcher.Node.Database.Tests;

public sealed class FeatureUsageBufferingTests : IDisposable
{
    private readonly string _dbPath =
        Path.Combine(Path.GetTempPath(), $"fs_node_usage_{Guid.NewGuid():N}.db");

    private readonly ServiceProvider _provider;
    private readonly ApplicationDto _app = new("UsageApp");
    private readonly FeatureDto _feature = new("UsageFeature", true);

    public FeatureUsageBufferingTests()
    {
        var services = new ServiceCollection();
        services.AddDatabase($"Data Source={_dbPath}");
        _provider = services.BuildServiceProvider();

        using var scope = _provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<FeaturesDataContext>().Database.Migrate();

        // Registration only bumps LastUsedAt (it is not a read); flush it so every test starts from a
        // written baseline with no reads counted.
        WithRepository(repository => repository.RegisterApplication(_app, [_feature]));
        WithRepository(repository => repository.FlushFeatureUsage());
    }

    [Fact]
    public void RecordFeatureUsage_DoesNotWriteUntilFlushed()
    {
        var lastUsedBefore = ReadLink().LastUsedAt;

        WithRepository(repository =>
        {
            repository.RecordFeatureUsage(_app, _feature.Name);
            repository.RecordFeatureUsage(_app, _feature.Name);
            repository.RecordFeatureUsage(_app, _feature.Name);
        });

        Assert.Equal(0, ReadTotalUseCount());
        Assert.Equal(lastUsedBefore, ReadLink().LastUsedAt);

        var written = 0;
        WithRepository(repository => written = repository.FlushFeatureUsage());

        Assert.Equal(1, written); // three uses of one link on one day aggregate into one entry
        Assert.Equal(3, ReadTotalUseCount());
        Assert.True(ReadLink().LastUsedAt > lastUsedBefore);
    }

    [Fact]
    public void RecordFeatureUsage_FromConcurrentRequests_CountsEveryUse()
    {
        // Previously every read did a read-modify-write of the daily counter outside a transaction,
        // so concurrent reads overwrote each other's increments.
        const int threads = 8;
        const int usesPerThread = 50;

        Parallel.For(0, threads, _ =>
        {
            for (var i = 0; i < usesPerThread; i++)
            {
                WithRepository(repository => repository.RecordFeatureUsage(_app, _feature.Name));
            }
        });
        WithRepository(repository => repository.FlushFeatureUsage());

        Assert.Equal(threads * usesPerThread, ReadTotalUseCount());
    }

    [Fact]
    public void RecordFeatureUsage_RestoresPendingLinkImmediately()
    {
        WithContext(context =>
        {
            var link = context.ApplicationFeatures.Single();
            link.Status = EntityStatus.PendingDeletion;
            link.PendingDeletionSince = DateTime.UtcNow.AddDays(-1);
            context.SaveChanges();
        });

        // No flush: the state read that follows must already see the link as Active.
        var state = false;
        WithRepository(repository =>
        {
            repository.RecordFeatureUsage(_app, _feature.Name);
            state = repository.GetFeatureState(_app, _feature.Name);
        });

        Assert.True(state);
        Assert.Equal(EntityStatus.Active, ReadLink().Status);
    }

    [Fact]
    public void FlushFeatureUsage_RestoresLinkMarkedPendingAfterBufferedUse()
    {
        // A use buffered just before the sweep marks the link must win once it is written.
        WithRepository(repository => repository.RecordFeatureUsage(_app, _feature.Name));
        WithContext(context =>
        {
            var link = context.ApplicationFeatures.Single();
            link.Status = EntityStatus.PendingDeletion;
            link.PendingDeletionSince = DateTime.UtcNow.AddMinutes(-1);
            context.SaveChanges();
        });

        WithRepository(repository => repository.FlushFeatureUsage());

        var restored = ReadLink();
        Assert.Equal(EntityStatus.Active, restored.Status);
        Assert.Null(restored.PendingDeletionSince);
    }

    [Fact]
    public void FlushFeatureUsage_SkipsUsageOfPermanentlyDeletedFeature()
    {
        WithRepository(repository => repository.RecordFeatureUsage(_app, _feature.Name));
        WithContext(context =>
        {
            var link = context.ApplicationFeatures.Single();
            link.Status = EntityStatus.PendingDeletion;
            link.PendingDeletionSince = DateTime.UtcNow.AddDays(1); // marked after the buffered use
            context.SaveChanges();
        });
        WithRepository(repository => repository.PermanentlyDeleteFeature(_app.Name, _feature.Name));

        var written = 0;
        WithRepository(repository => written = repository.FlushFeatureUsage());

        Assert.Equal(1, written);
        WithContext(context => Assert.Empty(context.ApplicationFeatureUsage.AsNoTracking().ToList()));
    }

    [Fact]
    public void FlushFeatureUsage_SplitsCountsByUtcDay()
    {
        var link = ReadLink();
        var buffer = _provider.GetRequiredService<FeatureUsageBuffer>();
        var today = DateTime.UtcNow.Date;

        buffer.Record(link.Id, link.ApplicationId, link.FeatureId, today.AddTicks(-1), countAsRead: true); // yesterday 23:59:59.9999999
        buffer.Record(link.Id, link.ApplicationId, link.FeatureId, today.AddHours(1), countAsRead: true);
        buffer.Record(link.Id, link.ApplicationId, link.FeatureId, today.AddHours(2), countAsRead: true);
        WithRepository(repository => repository.FlushFeatureUsage());

        WithContext(context =>
        {
            var rows = context.ApplicationFeatureUsage.AsNoTracking().ToList();
            Assert.Equal(1, rows.Single(u => u.UsageDay == today.AddDays(-1)).UseCount);
            Assert.Equal(2, rows.Single(u => u.UsageDay == today).UseCount);
        });
    }

    [Fact]
    public void Requeue_MergesFailedEntriesWithUsageRecordedMeanwhile()
    {
        var buffer = new FeatureUsageBuffer();
        var usedAt = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);

        buffer.Record(1, 2, 3, usedAt, countAsRead: true);
        buffer.Record(1, 2, 3, usedAt, countAsRead: true);
        var failed = buffer.Drain();
        buffer.Record(1, 2, 3, usedAt.AddMinutes(5), countAsRead: true);
        buffer.Requeue(failed);

        var entry = Assert.Single(buffer.Drain());
        Assert.Equal(3, entry.Value.Count);
        Assert.Equal(usedAt.AddMinutes(5), entry.Value.LastUsedAt);
        Assert.Equal(0, buffer.Count);
    }

    [Theory]
    [InlineData(5, true)]  // SQLITE_BUSY
    [InlineData(6, true)]  // SQLITE_LOCKED
    [InlineData(19, false)] // SQLITE_CONSTRAINT
    public void IsDatabaseBusy_RecognizesLockErrors_AlsoWhenWrapped(int sqliteErrorCode, bool expected)
    {
        var sqliteException = new SqliteException("test", sqliteErrorCode);

        Assert.Equal(expected, sqliteException.IsDatabaseBusy());
        Assert.Equal(expected, new DbUpdateException("wrapped", sqliteException).IsDatabaseBusy());
        Assert.False(new InvalidOperationException("other").IsDatabaseBusy());
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

    private ApplicationFeatureEntity ReadLink()
    {
        ApplicationFeatureEntity link = null!;
        WithContext(context => link = context.ApplicationFeatures.AsNoTracking().Single());
        return link;
    }

    private long ReadTotalUseCount()
    {
        long total = 0;
        WithContext(context => total = context.ApplicationFeatureUsage.AsNoTracking().Sum(u => u.UseCount));
        return total;
    }
}
