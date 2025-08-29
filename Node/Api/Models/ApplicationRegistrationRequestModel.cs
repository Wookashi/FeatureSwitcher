namespace Wookashi.FeatureSwitcher.Node.Api.Models;

internal sealed class ApplicationRegistrationRequestModel
{
    public string AppName { get; set; } = String.Empty;
    public string Environment { get; set; } = String.Empty;
    public List<RegisterFeatureStateModel> Features { get; set; } = [];
}