namespace Wookashi.FeatureSwitcher.Abstraction.Models;

public class FeatureStateModel(string name, bool isEnabled)
{
    public string Name { get; set; } = name;
    public bool IsEnabled { get; set; } = isEnabled;
}