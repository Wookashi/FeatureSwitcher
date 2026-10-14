using System.Text.RegularExpressions;
using Wookashi.FeatureSwitcher.Client.Abstraction.Models;

namespace Wookashi.FeatureSwitcher.Client.Implementation;

/// <summary>
/// Checks feature and application names against <see cref="NameLimits"/>.
/// </summary>
internal static class NameValidator
{
    private static readonly char[] ForbiddenCharacters = ['/', '\\', '?', '#', '%'];

    private static readonly Regex RecommendedFormat = new("^[A-Za-z][A-Za-z0-9_.-]*$", RegexOptions.CultureInvariant);

    /// <summary>
    /// Throws <see cref="ArgumentException"/> for names that cannot work: empty, with leading or trailing
    /// whitespace, longer than <see cref="NameLimits.MaxLength"/>, or with characters that break URLs.
    /// </summary>
    /// <param name="kind">"Feature" or "Application", for the message.</param>
    /// <param name="name">The name to check.</param>
    /// <param name="paramName">Parameter name reported in the exception.</param>
    internal static void ValidateRequired(string kind, string name, string paramName)
    {
        if (name.Trim().Length == 0)
        {
            throw new ArgumentException($"{kind} name must not be empty.", paramName);
        }

        if (name.Trim().Length != name.Length)
        {
            throw new ArgumentException($"{kind} name '{name}' must not start or end with whitespace.", paramName);
        }

        if (name.Length > NameLimits.MaxLength)
        {
            throw new ArgumentException(
                $"{kind} name '{Shorten(name)}' is {name.Length} characters long, the limit is {NameLimits.MaxLength} " +
                $"(recommended: at most {NameLimits.RecommendedMaxLength}). Put details in the feature description instead.",
                paramName);
        }

        var invalid = name.FirstOrDefault(c => char.IsControl(c) || ForbiddenCharacters.Contains(c));
        if (invalid != default)
        {
            var shown = char.IsControl(invalid) ? $"control character U+{(int)invalid:X4}" : $"'{invalid}'";
            throw new ArgumentException(
                $"{kind} name '{name}' contains {shown}, which breaks the Node's URLs. " +
                "Use letters, digits, '_', '-' and '.'.",
                paramName);
        }
    }

    /// <summary>
    /// Why the name does not follow the recommended format, or <c>null</c> when it does.
    /// </summary>
    internal static string? RecommendationProblem(string name)
    {
        var problems = new List<string>();
        if (name.Length > NameLimits.RecommendedMaxLength)
        {
            problems.Add($"{name.Length} characters");
        }

        if (!RecommendedFormat.IsMatch(name))
        {
            var unexpected = name.FirstOrDefault(c => !(c < 128 && (char.IsLetterOrDigit(c) || c is '_' or '-' or '.')));
            problems.Add(unexpected != default
                ? $"contains '{unexpected}'"
                : "does not start with a letter");
        }

        return problems.Count == 0 ? null : string.Join(", ", problems);
    }

    private static string Shorten(string value) => value.Length <= 60 ? value : value.Substring(0, 57) + "...";
}
