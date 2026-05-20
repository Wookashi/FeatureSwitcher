namespace Wookashi.FeatureSwitcher.Manager.Abstraction.Database.Dtos;

public sealed class PendingApplicationDto
{
    public string ApplicationName { get; set; } = string.Empty;
    public DateTime LastUsedAt { get; set; }
    public DateTime PendingDeletionSince { get; set; }
}
