namespace Wookashi.FeatureSwitcher.Manager.Abstraction.Database.Dtos;

public sealed class PendingFeatureDto
{
    public string ApplicationName { get; set; } = string.Empty;
    public string FeatureName { get; set; } = string.Empty;
    public DateTime LastUsedAt { get; set; }
    public DateTime PendingDeletionSince { get; set; }
}
