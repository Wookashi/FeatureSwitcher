namespace Wookashi.FeatureSwitcher.Node.Api.Models;

internal sealed class RegisterFeaturesRequestModel
{
    internal string AppName { get; set; } = String.Empty;
    public string Environment { get; set; } = String.Empty;
    internal List<RegisterFeatureStateModel> Features { get; set; } = [];
}