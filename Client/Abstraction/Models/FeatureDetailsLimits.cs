namespace Wookashi.FeatureSwitcher.Client.Abstraction.Models;

/// <summary>
/// Limits for the optional feature details (<see cref="IFeatureDetails"/>): the description and links shown
/// in the Manager. Definitions that exceed them are rejected as soon as they are created, so problems
/// surface at application startup or in the first test that builds the feature list.
/// </summary>
public static class FeatureDetailsLimits
{
    /// <summary>
    /// Maximum length of <see cref="IFeatureDetails.Description"/> in characters, counted after
    /// normalization (line endings unified, trailing spaces and repeated blank lines removed).
    /// </summary>
    public const int MaxDescriptionLength = 1000;

    /// <summary>
    /// Maximum number of <see cref="IFeatureDetails.Links"/> per feature.
    /// </summary>
    public const int MaxLinks = 5;

    /// <summary>
    /// Maximum length of <see cref="FeatureLink.Url"/> (its <see cref="Uri.AbsoluteUri"/>) in characters.
    /// </summary>
    public const int MaxUrlLength = 2048;

    /// <summary>
    /// Maximum length of <see cref="FeatureLink.Title"/> in characters.
    /// </summary>
    public const int MaxLinkTitleLength = 100;
}
