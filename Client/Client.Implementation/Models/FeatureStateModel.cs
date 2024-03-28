namespace Wookashi.FeatureSwitcher.Client.Implementation.Models;

public record FeatureStateModel(string Name, bool InitialState)
{
    public string Name { get; } = Name;
    public bool InitialState { get; } = InitialState;
    public bool CurrentLocalState { get; set; } = InitialState;
}