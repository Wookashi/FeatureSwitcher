namespace Wookashi.FeatureSwitcher.Node.Abstraction.Database.Dtos;

/// <summary>
/// Feature data enriched with usage metadata and the application's feature details, returned by the
/// Manager-facing list endpoint. Distinct from <see cref="FeatureDto"/> which is the minimal shape used
/// for client registration and state reads.
/// </summary>
public sealed class FeatureWithUsageDto(
    string name,
    bool state,
    DateTime lastUsedAt,
    long usesLast7Days,
    string? description,
    IReadOnlyList<FeatureLinkDto> links)
{
    public string Name { get; } = name;
    public bool State { get; } = state;
    public DateTime LastUsedAt { get; } = lastUsedAt;
    public long UsesLast7Days { get; } = usesLast7Days;
    public string? Description { get; } = description;
    public IReadOnlyList<FeatureLinkDto> Links { get; } = links;

    /// <summary>
    /// UTC day of the application's last read of the feature within the retained read history;
    /// <c>null</c> when there is none.
    /// </summary>
    public DateTime? LastReadAt { get; init; }

    /// <summary>
    /// When the application first registered the feature.
    /// </summary>
    public DateTime RegisteredAt { get; init; }
}
