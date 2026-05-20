namespace Wookashi.FeatureSwitcher.Node.Abstraction.Database.Dtos;

public sealed class PendingApplicationDto(
    string applicationName,
    DateTime lastUsedAt,
    DateTime pendingDeletionSince)
{
    public string ApplicationName { get; } = applicationName;
    public DateTime LastUsedAt { get; } = lastUsedAt;
    public DateTime PendingDeletionSince { get; } = pendingDeletionSince;
}
