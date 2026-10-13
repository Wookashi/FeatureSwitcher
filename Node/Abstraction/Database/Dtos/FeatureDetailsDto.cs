namespace Wookashi.FeatureSwitcher.Node.Abstraction.Database.Dtos;

/// <summary>
/// Optional details an application registers for one of its features: a description and links shown
/// in the Manager. Stored per application-feature link, so applications sharing a flag each keep their own.
/// </summary>
public sealed class FeatureDetailsDto(string? description, IReadOnlyList<FeatureLinkDto> links)
{
    public string? Description { get; } = description;
    public IReadOnlyList<FeatureLinkDto> Links { get; } = links;
}

/// <summary>
/// A link registered with a feature: an absolute http(s) URL and an optional title.
/// </summary>
public sealed class FeatureLinkDto(string url, string? title)
{
    public string Url { get; } = url;
    public string? Title { get; } = title;
}
