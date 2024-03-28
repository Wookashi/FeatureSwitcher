namespace Wookashi.FeatureSwitcher.Node.Abstraction.Database.Dtos;

public sealed class FeatureDto(string featureName, bool state)
{
    public string Name { get; } = featureName;
    public bool State { get; } = state;
}