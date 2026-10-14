using Wookashi.FeatureSwitcher.Client.Abstraction;
using Wookashi.FeatureSwitcher.Client.Abstraction.Models;

namespace Wookashi.FeatureSwitcher.Client.Implementation;

/// <summary>
/// Default implementation of a feature state.
/// Use this class to define features that your application will check.
/// </summary>
/// <example>
/// <code>
/// var features = new List&lt;IFeatureStateModel&gt;
/// {
///     new FeatureStateModel("DarkMode", initialState: false),
///     new FeatureStateModel("NewCheckout", initialState: true)
///     {
///         // Optional details shown in the Manager:
///         Description = """
///             Replaces the legacy multi-step checkout with the single-page flow.
///
///             When OFF:
///             - customers are redirected to /checkout/legacy
///             - carts stay compatible with both flows
///             """,
///         Links =
///         [
///             "https://jira.example.com/browse/SHOP-1423",
///             new("Rollout plan", "https://wiki.example.com/checkout/rollout"),
///         ],
///     },
/// };
/// </code>
/// </example>
public class FeatureStateModel : IFeatureStateModel, IFeatureDetails
{
    private string? _description;
    private IReadOnlyList<FeatureLink> _links = [];

    /// <summary>
    /// The unique name of this feature.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The initial state of this feature (used when the node is unreachable).
    /// </summary>
    public bool InitialState { get; }

    /// <summary>
    /// The current cached state of this feature.
    /// Updated when the node is queried successfully.
    /// </summary>
    public bool CurrentLocalState { get; set; }

    /// <summary>
    /// Optional description shown in the Manager: what the feature does and what happens when it is
    /// switched off. Plain text with light formatting — a blank line starts a new paragraph, lines starting
    /// with <c>"- "</c> form a bullet list, <c>http(s)</c> URLs become links. A raw string literal
    /// (<c>"""…"""</c>) keeps it readable in code; indentation, line endings, trailing spaces and repeated
    /// blank lines are normalized.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// The normalized description is longer than <see cref="FeatureDetailsLimits.MaxDescriptionLength"/>
    /// characters or contains control characters other than line breaks and tabs.
    /// </exception>
    public string? Description
    {
        get => _description;
        set
        {
            var normalized = FeatureDetailsValidator.NormalizeDescription(value);
            FeatureDetailsValidator.ValidateDescription(Name, normalized);
            _description = normalized;
        }
    }

    /// <summary>
    /// Optional links shown in the Manager, e.g. a ticket, documentation, runbook or dashboard. A URL string
    /// or <see cref="Uri"/> converts to a <see cref="FeatureLink"/> implicitly; use
    /// <c>new("Title", "https://…")</c> for a link with a title. Setting <c>null</c> clears the links.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// More than <see cref="FeatureDetailsLimits.MaxLinks"/> links, a <c>null</c> entry or the same URL twice.
    /// </exception>
    public IReadOnlyList<FeatureLink> Links
    {
        get => _links;
        set
        {
            FeatureDetailsValidator.ValidateLinks(Name, value);
            _links = value is null ? [] : value.ToArray();
        }
    }

    /// <summary>
    /// Creates a new feature state.
    /// </summary>
    /// <param name="name">
    /// Unique name for this feature. Recommended: at most <see cref="NameLimits.RecommendedMaxLength"/>
    /// characters, ASCII letters, digits, <c>_</c>, <c>-</c> and <c>.</c>, starting with a letter; see
    /// <see cref="NameLimits"/>.
    /// </param>
    /// <param name="initialState">Initial state (defaults to false/disabled).</param>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is null.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="name"/> is empty, starts or ends with whitespace, is longer than
    /// <see cref="NameLimits.MaxLength"/> characters or contains characters that break URLs.
    /// </exception>
    public FeatureStateModel(string name, bool initialState = false)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        NameValidator.ValidateRequired("Feature", name, nameof(name));
        InitialState = initialState;
        CurrentLocalState = initialState;
    }
}
