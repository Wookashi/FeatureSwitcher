namespace Wookashi.FeatureSwitcher.Node.Abstraction.Database.Dtos;

public sealed class FeatureUpdateResultDto(string featureName, bool state, IReadOnlyList<string> affectedApplicationNames)
{
    public string FeatureName { get; } = featureName;
    public bool State { get; } = state;
    public IReadOnlyList<string> AffectedApplicationNames { get; } = affectedApplicationNames;
    public int AffectedApplications { get; } = affectedApplicationNames.Count;
    public bool IsShared { get; } = affectedApplicationNames.Count > 1;
}
