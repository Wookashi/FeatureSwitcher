namespace Wookashi.FeatureSwitcher.Node.Api.Models;

internal class RegisterFeatureStateModel(string featureName, bool initialState)
{
    public string FeatureName { get; } = featureName;
    public bool InitialState { get; } = initialState;

    /// <summary>
    /// Optional description of the feature; not sent by older clients.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Optional links; not sent by older clients.
    /// </summary>
    public List<RegisterFeatureLinkModel>? Links { get; set; }
}

internal sealed class RegisterFeatureLinkModel(string url, string? title)
{
    public string Url { get; } = url;
    public string? Title { get; } = title;
}
