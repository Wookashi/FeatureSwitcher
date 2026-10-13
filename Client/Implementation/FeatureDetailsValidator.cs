using System.Text;
using Wookashi.FeatureSwitcher.Client.Abstraction.Models;

namespace Wookashi.FeatureSwitcher.Client.Implementation;

/// <summary>
/// Normalizes and checks feature details against <see cref="FeatureDetailsLimits"/>. Used when a
/// <see cref="FeatureStateModel"/> is configured and again by <see cref="FeatureManager"/> for custom
/// <see cref="IFeatureDetails"/> implementations, so every definition is checked before it is registered.
/// </summary>
internal static class FeatureDetailsValidator
{
    /// <summary>
    /// Unifies line endings to <c>\n</c>, removes trailing whitespace on each line, collapses repeated
    /// blank lines into one and trims blank lines at the start and the end. Returns <c>null</c> for a
    /// missing or blank description.
    /// </summary>
    internal static string? NormalizeDescription(string? description)
    {
        if (description is null)
        {
            return null;
        }

        var lines = description.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var builder = new StringBuilder(description.Length);
        var pendingBlankLine = false;

        foreach (var rawLine in lines)
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

        return builder.Length == 0 ? null : builder.ToString();
    }

    /// <summary>
    /// Throws <see cref="ArgumentException"/> when the (normalized) description is too long or contains
    /// control characters other than line breaks and tabs.
    /// </summary>
    internal static void ValidateDescription(string featureName, string? description)
    {
        if (description is null)
        {
            return;
        }

        for (var i = 0; i < description.Length; i++)
        {
            var c = description[i];
            if (char.IsControl(c) && c != '\n' && c != '\t')
            {
                throw new ArgumentException(
                    $"Feature '{featureName}': description contains a control character (U+{(int)c:X4}) at position {i}. Only line breaks and tabs are allowed.",
                    nameof(IFeatureDetails.Description));
            }
        }

        if (description.Length > FeatureDetailsLimits.MaxDescriptionLength)
        {
            throw new ArgumentException(
                $"Feature '{featureName}': description is {description.Length} characters long, the limit is {FeatureDetailsLimits.MaxDescriptionLength}. Keep the description short and move details to Links.",
                nameof(IFeatureDetails.Description));
        }
    }

    /// <summary>
    /// Throws <see cref="ArgumentException"/> when there are too many links, a <c>null</c> entry or the
    /// same URL twice. Each <see cref="FeatureLink"/> validates its own URL and title when created.
    /// </summary>
    internal static void ValidateLinks(string featureName, IReadOnlyList<FeatureLink>? links)
    {
        if (links is null)
        {
            return;
        }

        if (links.Count > FeatureDetailsLimits.MaxLinks)
        {
            throw new ArgumentException(
                $"Feature '{featureName}': {links.Count} links given, the limit is {FeatureDetailsLimits.MaxLinks}. Link to an overview page that collects the rest.",
                nameof(IFeatureDetails.Links));
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var link in links)
        {
            if (link is null)
            {
                throw new ArgumentException(
                    $"Feature '{featureName}': links must not contain null.",
                    nameof(IFeatureDetails.Links));
            }

            if (!seen.Add(link.Url.AbsoluteUri))
            {
                throw new ArgumentException(
                    $"Feature '{featureName}': link '{link.Url.AbsoluteUri}' is listed more than once.",
                    nameof(IFeatureDetails.Links));
            }
        }
    }
}
