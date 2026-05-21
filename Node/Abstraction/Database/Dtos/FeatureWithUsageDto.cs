namespace Wookashi.FeatureSwitcher.Node.Abstraction.Database.Dtos;

/// <summary>
/// Feature data enriched with usage metadata, returned by the Manager-facing list endpoint.
/// Distinct from <see cref="FeatureDto"/> which is the minimal shape used for client registration
/// and state reads.
/// </summary>
public sealed class FeatureWithUsageDto(string name, bool state, DateTime lastUsedAt, long usesLast7Days)
{
    public string Name { get; } = name;
    public bool State { get; } = state;
    public DateTime LastUsedAt { get; } = lastUsedAt;
    public long UsesLast7Days { get; } = usesLast7Days;
}
