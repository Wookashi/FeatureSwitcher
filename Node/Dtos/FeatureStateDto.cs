namespace Wookashi.FeatureSwitcher.Node.Dtos;

internal class FeatureStateDto( string appName, string environment, string featureName, bool initialState)
{
    internal string AppName { get; set; } = appName;
    internal string Environment { get; set; } = environment;
    internal string FeatureName { get; } = featureName;
    internal bool InitialState { get; } = initialState;
}