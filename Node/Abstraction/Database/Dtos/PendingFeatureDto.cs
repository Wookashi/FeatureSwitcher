namespace Wookashi.FeatureSwitcher.Node.Abstraction.Database.Dtos;

public sealed class PendingFeatureDto(
    string applicationName,
    string featureName,
    DateTime lastUsedAt,
    DateTime pendingDeletionSince)
{
    public string ApplicationName { get; } = applicationName;
    public string FeatureName { get; } = featureName;
    public DateTime LastUsedAt { get; } = lastUsedAt;
    public DateTime PendingDeletionSince { get; } = pendingDeletionSince;
}
