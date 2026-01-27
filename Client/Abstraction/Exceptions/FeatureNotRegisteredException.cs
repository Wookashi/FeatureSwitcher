namespace Wookashi.FeatureSwitcher.Client.Abstraction.Exceptions;

/// <summary>
/// Thrown when trying to check a feature that was not registered during initialization.
/// </summary>
public sealed class FeatureNotRegisteredException(string message) : FeatureSwitcherException(message, 1);