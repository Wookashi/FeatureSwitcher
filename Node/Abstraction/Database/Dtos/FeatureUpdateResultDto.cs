namespace Wookashi.FeatureSwitcher.Node.Abstraction.Database.Dtos;

public sealed class FeatureUpdateResultDto(string featureName, bool state, int affectedApplications)
{
    public string FeatureName { get; } = featureName;
    public bool State { get; } = state;
    public int AffectedApplications { get; } = affectedApplications;
    public bool IsShared { get; } = affectedApplications > 1;
}
