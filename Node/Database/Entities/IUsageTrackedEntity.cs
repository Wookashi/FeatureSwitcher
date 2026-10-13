namespace Wookashi.FeatureSwitcher.Node.Database.Entities;

/// <summary>
/// Entity whose lifecycle is driven by usage: it is marked PendingDeletion when unused for too long
/// and restored when used again.
/// </summary>
internal interface IUsageTrackedEntity
{
    EntityStatus Status { get; set; }
    DateTime? PendingDeletionSince { get; set; }
    DateTime LastUsedAt { get; set; }
}
