namespace Wookashi.FeatureSwitcher.Node.Models;

internal class RegisterFeatureStateModel(string featureName, bool initialState)
{
    internal string FeatureName { get; } = featureName;
    internal bool InitialState { get; } = initialState;
}