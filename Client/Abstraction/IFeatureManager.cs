using Wookashi.FeatureSwitcher.Client.Abstraction.Models;

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

    /// <summary>
    /// Checks if a feature is enabled using a strongly-typed reference to the registered flag.
    /// Prefer this overload over the string-based one — declare your flags as static fields on a class
    /// (e.g., <c>Flags.DarkMode</c>) and pass them by reference to avoid typos and enable refactoring safety.
    /// </summary>
    /// <param name="feature">The feature flag instance (must be one of the flags registered on this manager).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>True if the feature is enabled, false otherwise.</returns>
    Task<bool> IsFeatureEnabledAsync(IFeatureStateModel feature, CancellationToken cancellationToken = default);
}
