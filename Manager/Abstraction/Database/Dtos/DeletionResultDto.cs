namespace Wookashi.FeatureSwitcher.Manager.Abstraction.Database.Dtos;

public sealed class DeletionResultDto
{
    public DateTime LastUsedAt { get; set; }
    public DateTime PendingDeletionSince { get; set; }
}
