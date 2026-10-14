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

    /// <summary>
    /// UTC day of the application's last read within the node's retained read history; <c>null</c>
    /// when there is none or when the node is older.
    /// </summary>
    public DateTime? LastReadAt { get; set; }

    /// <summary>
    /// When the application first registered the feature; <c>null</c> when the node is older, in which
    /// case the UI does not report the feature as unread.
    /// </summary>
    public DateTime? RegisteredAt { get; set; }
}

public sealed class FeatureLinkDto
{
    public string Url { get; set; } = string.Empty;
    public string? Title { get; set; }
}
