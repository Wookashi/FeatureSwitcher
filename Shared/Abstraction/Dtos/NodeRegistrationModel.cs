namespace Wookashi.FeatureSwitcher.Shared.Abstraction.Dtos;

public sealed class NodeRegistrationModel
{
    public string AppName { get; set; } = String.Empty;
    public string Environment { get; set; } = String.Empty;
    public List<FeatureStateModel> Features { get; set; } = [];
    public class FeatureStateModel(string featureName, bool state)
    {
        public string FeatureName { get; } = featureName;
        public bool State { get; } = state;
    }
}