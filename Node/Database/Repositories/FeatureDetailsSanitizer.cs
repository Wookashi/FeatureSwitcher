using System.Text;
using System.Text.Json;
using Wookashi.FeatureSwitcher.Node.Abstraction.Database.Dtos;

namespace Wookashi.FeatureSwitcher.Node.Database.Repositories;

/// <summary>
/// Brings feature details sent with a registration within the limits before they are stored.
/// </summary>
/// <remarks>
/// The .NET client already rejects definitions that break these limits, but a registration can come from
/// any client. Unlike the client, the Node never rejects a registration because of its details: they are
/// an addition, while the flags must keep working. Problems are fixed (normalized, truncated or dropped)
/// and reported to the caller for logging. The limits mirror <c>FeatureDetailsLimits</c> in the client.
/// </remarks>
public static class FeatureDetailsSanitizer
{
    public const int MaxDescriptionLength = 1000;
    public const int MaxLinks = 5;
    public const int MaxUrlLength = 2048;
    public const int MaxLinkTitleLength = 100;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Returns the details to store, or <c>null</c> when nothing usable remains. Every adjustment is added
    /// to <paramref name="problems"/>.
    /// </summary>
    public static FeatureDetailsDto? Sanitize(FeatureDetailsDto? details, ICollection<string> problems)
    {
        if (details is null)
        {
            return null;
        }

        var description = SanitizeDescription(details.Description, problems);
        var links = SanitizeLinks(details.Links, problems);

        return description is null && links.Count == 0 ? null : new FeatureDetailsDto(description, links);
    }

    /// <summary>
    /// Serializes links for <c>ApplicationFeatureEntity.LinksJson</c>; <c>null</c> when there are none.
    /// </summary>
    internal static string? SerializeLinks(IReadOnlyList<FeatureLinkDto>? links) =>
        links is null || links.Count == 0 ? null : JsonSerializer.Serialize(links, JsonOptions);

    /// <summary>
    /// Reads <c>ApplicationFeatureEntity.LinksJson</c>; unreadable content yields no links rather than
    /// failing the request.
    /// </summary>
    internal static IReadOnlyList<FeatureLinkDto> DeserializeLinks(string? json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<FeatureLinkDto>>(json, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static string? SanitizeDescription(string? description, ICollection<string> problems)
    {
        if (description is null)
        {
            return null;
        }

        var text = Normalize(description);
        if (text.Any(c => char.IsControl(c) && c != '\n' && c != '\t'))
        {
            text = new string(text.Where(c => !char.IsControl(c) || c == '\n' || c == '\t').ToArray());
            problems.Add("description contained control characters; they were removed");
        }

        if (text.Length > MaxDescriptionLength)
        {
            problems.Add($"description was {text.Length} characters long and was truncated to {MaxDescriptionLength}");
            text = text[..(MaxDescriptionLength - 1)].TrimEnd() + "…";
        }

        return text.Length == 0 ? null : text;
    }

    private static List<FeatureLinkDto> SanitizeLinks(IReadOnlyList<FeatureLinkDto>? links, ICollection<string> problems)
    {
        var result = new List<FeatureLinkDto>();
        if (links is null)
        {
            return result;
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var link in links)
        {
            if (link?.Url is null
                || !Uri.TryCreate(link.Url.Trim(), UriKind.Absolute, out var url)
                || (url.Scheme != Uri.UriSchemeHttp && url.Scheme != Uri.UriSchemeHttps)
                || url.Host.Length == 0)
            {
                problems.Add("a link that is not an absolute http(s) URL was dropped");
                continue;
            }

            if (url.UserInfo.Length > 0)
            {
                problems.Add($"a link to '{url.Host}' contained credentials and was dropped");
                continue;
            }

            if (url.AbsoluteUri.Length > MaxUrlLength)
            {
                problems.Add($"a link to '{url.Host}' was longer than {MaxUrlLength} characters and was dropped");
                continue;
            }

            if (!seen.Add(url.AbsoluteUri))
            {
                problems.Add($"duplicate link '{url.AbsoluteUri}' was dropped");
                continue;
            }

            if (result.Count == MaxLinks)
            {
                problems.Add($"more than {MaxLinks} links were registered; the rest were dropped");
                break;
            }

            result.Add(new FeatureLinkDto(url.AbsoluteUri, SanitizeTitle(link.Title, problems)));
        }

        return result;
    }

    private static string? SanitizeTitle(string? title, ICollection<string> problems)
    {
        if (title is null)
        {
            return null;
        }

        var text = new string(title.Select(c => char.IsControl(c) ? ' ' : c).ToArray()).Trim();
        if (text.Length > MaxLinkTitleLength)
        {
            problems.Add($"a link title was longer than {MaxLinkTitleLength} characters and was truncated");
            text = text[..(MaxLinkTitleLength - 1)].TrimEnd() + "…";
        }

        return text.Length == 0 ? null : text;
    }

    // Same normalization as the client: unified line endings, no trailing whitespace, at most one blank
    // line in a row, no blank lines at the start or the end.
    private static string Normalize(string text)
    {
        var builder = new StringBuilder(text.Length);
        var pendingBlankLine = false;

        foreach (var rawLine in text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
        {
            var line = rawLine.TrimEnd();
            if (line.Length == 0)
            {
                pendingBlankLine = builder.Length > 0;
                continue;
            }

            if (builder.Length > 0)
            {
                builder.Append(pendingBlankLine ? "\n\n" : "\n");
            }

            builder.Append(line);
            pendingBlankLine = false;
        }

        return builder.ToString();
    }
}
