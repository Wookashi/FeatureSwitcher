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
}
