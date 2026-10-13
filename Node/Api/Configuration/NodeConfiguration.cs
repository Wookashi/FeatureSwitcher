namespace Wookashi.FeatureSwitcher.Node.Api.Configuration;

internal sealed class NodeConfiguration
{
    public string? Environment { get; set; }
    public string? ConnectionString { get; set; }
    public string? Name { get; set; }
    public string? Address { get; set; }

    /// <summary>
    /// How long a feature can go without any registration or state-read before the sweep marks it
    /// PendingDeletion. Defaults to 30 days.
    /// </summary>
    public TimeSpan FeatureStaleAfter { get; set; } = TimeSpan.FromDays(30);

    /// <summary>
    /// How often the soft-delete sweep runs. Defaults to 24 hours.
    /// </summary>
    public TimeSpan FeatureCleanupInterval { get; set; } = TimeSpan.FromHours(24);

    /// <summary>
    /// How often runtime memory/GC statistics are logged. Disabled when not set (or zero) so the log is
    /// not cluttered by default; enable it (e.g. 00:15:00) when investigating memory issues. The
    /// one-time runtime summary at startup is always logged.
    /// </summary>
    public TimeSpan DiagnosticsInterval { get; set; } = TimeSpan.Zero;

    internal static readonly TimeSpan DefaultUsageFlushInterval = TimeSpan.FromSeconds(60);

    /// <summary>
    /// How often buffered feature usage (LastUsedAt, daily use counts) is written to the database.
    /// Flag-state reads only record usage in memory, so they never wait for a database write. A hard
    /// kill loses at most this much usage data; flag states are not affected. Defaults to 60 seconds.
    /// </summary>
    public TimeSpan UsageFlushInterval { get; set; } = DefaultUsageFlushInterval;
}
