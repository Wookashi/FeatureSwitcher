namespace Wookashi.FeatureSwitcher.Client.Abstraction;

/// <summary>
/// Interface for checking feature flag states.
/// </summary>
public interface IFeatureManager
{
    /// <summary>
    /// Checks if a feature is enabled.
    /// </summary>
    /// <param name="featureName">Name of the feature to check.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>True if the feature is enabled, false otherwise.</returns>
    Task<bool> IsFeatureEnabledAsync(string featureName, CancellationToken cancellationToken = default);
}
