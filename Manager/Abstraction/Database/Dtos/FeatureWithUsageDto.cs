namespace Wookashi.FeatureSwitcher.Manager.Abstraction.Database.Dtos;

public sealed class FeatureWithUsageDto
{
    public string Name { get; set; } = string.Empty;
    public bool State { get; set; }
    public DateTime LastUsedAt { get; set; }
    public long UsesLast7Days { get; set; }
}
