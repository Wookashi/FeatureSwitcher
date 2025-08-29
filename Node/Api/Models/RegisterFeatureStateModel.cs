namespace Wookashi.FeatureSwitcher.Node.Api.Models;

internal class RegisterFeatureStateModel(string featureName, bool initialState)
{
    public string FeatureName { get; } = featureName;
    public bool InitialState { get; } = initialState;
}