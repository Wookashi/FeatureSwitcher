namespace Wookashi.FeatureSwitcher.Manager.Abstraction.Database.Dtos;

public sealed class FeatureDto(string name, bool state)
{
    public string Name { get; } = name;
    public bool State { get; } = state;
}