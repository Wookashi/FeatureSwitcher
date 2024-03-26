namespace Wookashi.FeatureSwitcher.Node.Models;

internal sealed class RegisterFeaturesRequestModel
{
    internal string AppName { get; set; }
    public string Environment { get; set; }
    internal List<RegisterFeatureStateModel> Features { get; set; }
}