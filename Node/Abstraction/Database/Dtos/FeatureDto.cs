namespace Wookashi.FeatureSwitcher.Node.Abstraction.Database.Dtos;

public sealed class FeatureDto(string featureName, bool state)
{
    public string Name { get; } = featureName;
    public bool State { get; } = state;

    /// <summary>
    /// Details sent with a registration; <c>null</c> when the application provides none, which clears
    /// previously registered details.
    /// </summary>
    public FeatureDetailsDto? Details { get; init; }
}