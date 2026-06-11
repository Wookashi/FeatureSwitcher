namespace Wookashi.FeatureSwitcher.Node.Abstraction.Database.Dtos;

/// <summary>
/// Metadata returned when a feature or application-feature link is permanently deleted, so the
/// Manager can write a complete audit-log entry.
/// </summary>
public sealed class DeletionResultDto(
    DateTime lastUsedAt,
    DateTime pendingDeletionSince,
    int removedApplicationFeatureLinks,
    int deletedFeatures)
{
    public DateTime LastUsedAt { get; } = lastUsedAt;
    public DateTime PendingDeletionSince { get; } = pendingDeletionSince;
    public int RemovedApplicationFeatureLinks { get; } = removedApplicationFeatureLinks;
    public int DeletedFeatures { get; } = deletedFeatures;
}
