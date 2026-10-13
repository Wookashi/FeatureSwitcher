namespace Wookashi.FeatureSwitcher.Manager.Abstraction.Database.Dtos;

public sealed class FeatureWithUsageDto
{
    public string Name { get; set; } = string.Empty;
    public bool State { get; set; }
    public DateTime LastUsedAt { get; set; }
    public long UsesLast7Days { get; set; }

    /// <summary>
    /// Description registered by the application; <c>null</c> when none or when the node is older.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Links registered by the application; empty when none or when the node is older.
    /// </summary>
    public List<FeatureLinkDto> Links { get; set; } = [];
}

public sealed class FeatureLinkDto
{
    public string Url { get; set; } = string.Empty;
    public string? Title { get; set; }
}
