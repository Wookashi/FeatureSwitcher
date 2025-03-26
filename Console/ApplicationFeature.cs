using Wookashi.FeatureSwitcher.Client.Abstraction;

namespace Wookashi.FeatureSwitcher.Console;

internal sealed class ApplicationFeature (string name, bool initialState, Uri link) : IFeatureStateModel
{
    public string Name { get; } = name;
    public bool InitialState { get; } = initialState;
    public bool CurrentLocalState { get; set; } = initialState;
    internal Uri Link { get; set; } = link;
    
}