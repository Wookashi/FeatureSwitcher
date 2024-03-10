namespace Wookashi.FeatureSwitcher.Client.Implementation.Models;

public record FeatureStateModel(string Name, bool IsEnabled)
{
    public string Name { get; } = Name;
    public bool IsEnabled { get; } = IsEnabled;
}