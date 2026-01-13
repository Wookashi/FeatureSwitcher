namespace Wookashi.FeatureSwitcher.Node.Api.Models;

internal sealed class ApplicationRegistrationRequestModel
{
    public string AppName { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public List<RegisterFeatureStateModel> Features { get; set; } = [];
}