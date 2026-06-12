namespace Wookashi.FeatureSwitcher.Manager.Abstraction.Database.Dtos;

public sealed class FeatureUpdateResultDto
{
    public string FeatureName { get; set; } = string.Empty;
    public bool State { get; set; }
    public int AffectedApplications { get; set; }
    public bool IsShared { get; set; }
    public IReadOnlyList<string> AffectedApplicationNames { get; set; } = [];
}
