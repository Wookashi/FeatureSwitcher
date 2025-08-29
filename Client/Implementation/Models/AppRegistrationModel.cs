namespace Wookashi.FeatureSwitcher.Client.Implementation.Models;

public sealed class AppRegistrationModel
{
    public string AppName { get; set; } = String.Empty;
    public string Environment { get; set; } = String.Empty;
    public List<RegisterFeatureStateModel> Features { get; set; } = [];
    public class RegisterFeatureStateModel(string featureName, bool initialState)
    {
        public string FeatureName { get; } = featureName;
        public bool InitialState { get; } = initialState;
    }
}