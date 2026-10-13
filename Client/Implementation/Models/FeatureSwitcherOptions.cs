using System.Text.Json.Serialization;

namespace Wookashi.FeatureSwitcher.Client.Implementation.Models;

/// <summary>
/// Payload sent to the Node to register an application and its features. Used internally by
/// <see cref="FeatureManager.RegisterFeaturesOnNodeAsync"/>.
/// </summary>
public sealed class AppRegistrationModel
{
    /// <summary>
    /// Name of the registering application.
    /// </summary>
    public string AppName { get; set; } = string.Empty;

    /// <summary>
    /// Environment of the application; must match the Node's environment.
    /// </summary>
    public string Environment { get; set; } = string.Empty;

    /// <summary>
    /// Features used by the application.
    /// </summary>
    public List<RegisterFeatureStateModel> Features { get; set; } = [];

    /// <summary>
    /// A single feature in the registration payload.
    /// </summary>
    /// <param name="featureName">Name of the feature.</param>
    /// <param name="initialState">State used when the feature is created on the Node.</param>
    public class RegisterFeatureStateModel(string featureName, bool initialState)
    {
        /// <summary>
        /// Name of the feature.
        /// </summary>
        public string FeatureName { get; } = featureName;

        /// <summary>
        /// State used when the feature is created on the Node.
        /// </summary>
        public bool InitialState { get; } = initialState;

        /// <summary>
        /// Normalized description; omitted from the payload when not set, so Nodes that don't know the
        /// field receive the same payload as before.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Description { get; set; }

        /// <summary>
        /// Links; omitted from the payload when there are none.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<RegisterFeatureLinkModel>? Links { get; set; }
    }

    /// <summary>
    /// A single link in the registration payload.
    /// </summary>
    /// <param name="url">Absolute URL, as <see cref="Uri.AbsoluteUri"/>.</param>
    /// <param name="title">Optional label.</param>
    public class RegisterFeatureLinkModel(string url, string? title)
    {
        /// <summary>
        /// Absolute URL, as <see cref="Uri.AbsoluteUri"/>.
        /// </summary>
        public string Url { get; } = url;

        /// <summary>
        /// Optional label; omitted from the payload when not set.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Title { get; } = title;
    }
}
