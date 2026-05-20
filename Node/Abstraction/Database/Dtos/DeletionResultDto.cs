namespace Wookashi.FeatureSwitcher.Node.Abstraction.Database.Dtos;

/// <summary>
/// Metadata returned when a feature or application is permanently deleted, so the Manager can
/// write a complete audit-log entry.
/// </summary>
public sealed class DeletionResultDto(DateTime lastUsedAt, DateTime pendingDeletionSince)
{
    public DateTime LastUsedAt { get; } = lastUsedAt;
    public DateTime PendingDeletionSince { get; } = pendingDeletionSince;
}
