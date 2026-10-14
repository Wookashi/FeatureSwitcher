namespace Wookashi.FeatureSwitcher.Client.Abstraction.Models;

/// <summary>
/// Rules for feature and application names. Both are part of the Node's URLs and are shown in the
/// Manager's feature matrix.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>
/// <b>Recommended</b>: at most <see cref="RecommendedMaxLength"/> characters, ASCII letters, digits,
/// <c>_</c>, <c>-</c> and <c>.</c>, starting with a letter (e.g. <c>NewCheckout</c>, <c>billing.retry-v2</c>).
/// Other names still work, but a warning is logged at startup; the next major version will require the
/// recommended format. Put details in <see cref="IFeatureDetails.Description"/> instead of the name.
/// </description></item>
/// <item><description>
/// <b>Required</b>: not empty, no leading or trailing whitespace, at most <see cref="MaxLength"/>
/// characters, and none of <c>/ \ ? # %</c> or control characters, which break the Node's URLs.
/// Violations throw <see cref="ArgumentException"/> when the feature or the feature manager is created.
/// </description></item>
/// </list>
/// </remarks>
public static class NameLimits
{
    /// <summary>
    /// Recommended maximum length of feature and application names.
    /// </summary>
    public const int RecommendedMaxLength = 50;

    /// <summary>
    /// Maximum length of feature and application names.
    /// </summary>
    public const int MaxLength = 100;
}
