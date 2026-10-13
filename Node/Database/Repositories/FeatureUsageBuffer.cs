using System.Runtime.InteropServices;

namespace Wookashi.FeatureSwitcher.Node.Database.Repositories;

/// <summary>
/// In-memory aggregation of feature usage (reads and registrations), shared by all requests.
/// Recording a use is a dictionary update instead of a database write, so the hot flag-state read
/// path never has to wait for SQLite's single writer. <see cref="FeatureRepository.FlushFeatureUsage"/>
/// periodically writes the aggregate in one transaction.
/// Memory is bounded by the number of distinct (application-feature link, UTC day) pairs used
/// between flushes — repeated uses only increment a counter.
/// </summary>
internal sealed class FeatureUsageBuffer
{
    internal readonly record struct UsageKey(int ApplicationFeatureId, int ApplicationId, int FeatureId, DateTime UsageDay);

    internal record struct UsageEntry(long Count, DateTime LastUsedAt);

    private readonly Lock _entriesLock = new();
    private Dictionary<UsageKey, UsageEntry> _entries = new();

    /// <summary>
    /// Serializes flushes, so buffered counts are never written twice or applied concurrently.
    /// </summary>
    internal Lock FlushLock { get; } = new();

    internal int Count
    {
        get
        {
            lock (_entriesLock)
            {
                return _entries.Count;
            }
        }
    }

    internal void Record(int applicationFeatureId, int applicationId, int featureId, DateTime usedAtUtc)
    {
        var key = new UsageKey(applicationFeatureId, applicationId, featureId, usedAtUtc.Date);
        lock (_entriesLock)
        {
            ref var entry = ref CollectionsMarshal.GetValueRefOrAddDefault(_entries, key, out _);
            entry.Count++;
            if (usedAtUtc > entry.LastUsedAt)
            {
                entry.LastUsedAt = usedAtUtc;
            }
        }
    }

    /// <summary>
    /// Takes everything recorded so far and leaves the buffer empty.
    /// </summary>
    internal IReadOnlyDictionary<UsageKey, UsageEntry> Drain()
    {
        lock (_entriesLock)
        {
            var drained = _entries;
            _entries = new Dictionary<UsageKey, UsageEntry>();
            return drained;
        }
    }

    /// <summary>
    /// Puts back entries whose flush failed, merged with anything recorded in the meantime, so a
    /// failed write is retried on the next flush instead of losing the usage.
    /// </summary>
    internal void Requeue(IReadOnlyDictionary<UsageKey, UsageEntry> entries)
    {
        lock (_entriesLock)
        {
            foreach (var (key, value) in entries)
            {
                ref var entry = ref CollectionsMarshal.GetValueRefOrAddDefault(_entries, key, out _);
                entry.Count += value.Count;
                if (value.LastUsedAt > entry.LastUsedAt)
                {
                    entry.LastUsedAt = value.LastUsedAt;
                }
            }
        }
    }
}
