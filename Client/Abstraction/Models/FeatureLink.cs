namespace Wookashi.FeatureSwitcher.Client.Abstraction.Models;

/// <summary>
/// A link shown with a feature in the Manager, e.g. a ticket, documentation, runbook or dashboard.
/// </summary>
/// <remarks>
/// A URL <see cref="string"/> or <see cref="Uri"/> converts to a link implicitly, so links without a title
/// can be listed directly; the Manager then shows the host and path as the label:
/// <code>
/// Links =
/// [
///     "https://jira.example.com/browse/SHOP-1423",
///     new("Rollout plan", "https://wiki.example.com/checkout/rollout"),
/// ],
/// </code>
/// The URL must be an absolute <c>http</c> or <c>https</c> URL without credentials, at most
/// <see cref="FeatureDetailsLimits.MaxUrlLength"/> characters long. An invalid link throws
/// <see cref="ArgumentException"/> as soon as it is created.
/// </remarks>
public sealed class FeatureLink
{
    /// <summary>
    /// The link target: an absolute <c>http</c> or <c>https</c> URL.
    /// </summary>
    public Uri Url { get; }

    /// <summary>
    /// Optional label shown instead of the URL: a single line of at most
    /// <see cref="FeatureDetailsLimits.MaxLinkTitleLength"/> characters.
    /// </summary>
    public string? Title { get; }

    /// <summary>
    /// Creates a link without a title.
    /// </summary>
    /// <param name="url">Absolute <c>http</c> or <c>https</c> URL.</param>
    /// <exception cref="ArgumentNullException"><paramref name="url"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="url"/> is not a valid link URL.</exception>
    public FeatureLink(Uri url)
        : this(null, url)
    {
    }

    /// <summary>
    /// Creates a link without a title.
    /// </summary>
    /// <param name="url">Absolute <c>http</c> or <c>https</c> URL.</param>
    /// <exception cref="ArgumentNullException"><paramref name="url"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="url"/> is not a valid link URL.</exception>
    public FeatureLink(string url)
        : this(null, url)
    {
    }

    /// <summary>
    /// Creates a link with a title.
    /// </summary>
    /// <param name="title">Label shown instead of the URL; <c>null</c> or empty means no title.</param>
    /// <param name="url">Absolute <c>http</c> or <c>https</c> URL.</param>
    /// <exception cref="ArgumentNullException"><paramref name="url"/> is null.</exception>
    /// <exception cref="ArgumentException">The URL or the title does not meet the limits.</exception>
    public FeatureLink(string? title, Uri url)
    {
        if (url is null)
        {
            throw new ArgumentNullException(nameof(url));
        }

        Url = ValidateUrl(url.IsAbsoluteUri ? url : null, url.OriginalString, title);
        Title = ValidateTitle(title);
    }

    /// <summary>
    /// Creates a link with a title.
    /// </summary>
    /// <param name="title">Label shown instead of the URL; <c>null</c> or empty means no title.</param>
    /// <param name="url">Absolute <c>http</c> or <c>https</c> URL.</param>
    /// <exception cref="ArgumentNullException"><paramref name="url"/> is null.</exception>
    /// <exception cref="ArgumentException">The URL or the title does not meet the limits.</exception>
    public FeatureLink(string? title, string url)
    {
        if (url is null)
        {
            throw new ArgumentNullException(nameof(url));
        }

        Uri.TryCreate(url.Trim(), UriKind.Absolute, out var parsed);
        Url = ValidateUrl(parsed, url, title);
        Title = ValidateTitle(title);
    }

    /// <summary>
    /// Converts a URL to a link without a title.
    /// </summary>
    /// <param name="url">Absolute <c>http</c> or <c>https</c> URL.</param>
    public static implicit operator FeatureLink(string url) => new(url);

    /// <summary>
    /// Converts a URL to a link without a title.
    /// </summary>
    /// <param name="url">Absolute <c>http</c> or <c>https</c> URL.</param>
    public static implicit operator FeatureLink(Uri url) => new(url);

    /// <summary>
    /// Returns the title and the URL, or just the URL when there is no title.
    /// </summary>
    public override string ToString() => Title is null ? Url.AbsoluteUri : $"{Title} ({Url.AbsoluteUri})";

    private static Uri ValidateUrl(Uri? parsed, string original, string? title)
    {
        if (parsed is null || !IsHttp(parsed) || parsed.Host.Length == 0)
        {
            // new("https://…", "Title") instead of new("Title", "https://…")
            var swapped = title is not null
                && Uri.TryCreate(title.Trim(), UriKind.Absolute, out var titleAsUrl)
                && IsHttp(titleAsUrl);

            throw new ArgumentException(
                $"Link '{Shorten(original)}' must be an absolute http or https URL, e.g. \"https://jira.example.com/browse/SHOP-1\"."
                + (swapped ? " The arguments look swapped: the title goes first, new(\"Title\", \"https://…\")." : string.Empty),
                "url");
        }

        if (parsed.UserInfo.Length > 0)
        {
            throw new ArgumentException(
                $"Link to '{parsed.Host}' must not contain credentials (user:password@): links are visible to every Manager user.",
                "url");
        }

        if (parsed.AbsoluteUri.Length > FeatureDetailsLimits.MaxUrlLength)
        {
            throw new ArgumentException(
                $"Link '{Shorten(parsed.AbsoluteUri)}' is {parsed.AbsoluteUri.Length} characters long, the limit is {FeatureDetailsLimits.MaxUrlLength}.",
                "url");
        }

        return parsed;
    }

    private static string? ValidateTitle(string? title)
    {
        var trimmed = title?.Trim();
        if (trimmed is null || trimmed.Length == 0)
        {
            return null;
        }

        if (trimmed.Any(char.IsControl))
        {
            throw new ArgumentException(
                $"Link title '{Shorten(trimmed)}' must be a single line without control characters.",
                nameof(title));
        }

        if (trimmed.Length > FeatureDetailsLimits.MaxLinkTitleLength)
        {
            throw new ArgumentException(
                $"Link title '{Shorten(trimmed)}' is {trimmed.Length} characters long, the limit is {FeatureDetailsLimits.MaxLinkTitleLength}. Put longer explanations in the description.",
                nameof(title));
        }

        return trimmed;
    }

    private static bool IsHttp(Uri url) => url.Scheme == Uri.UriSchemeHttp || url.Scheme == Uri.UriSchemeHttps;

    private static string Shorten(string value) => value.Length <= 80 ? value : value.Substring(0, 77) + "...";
}
