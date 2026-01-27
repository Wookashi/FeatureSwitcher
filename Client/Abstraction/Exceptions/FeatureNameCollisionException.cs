namespace Wookashi.FeatureSwitcher.Client.Abstraction.Exceptions;

/// <summary>
/// Thrown when multiple features have the same name.
/// </summary>
public sealed class FeatureNameCollisionException(string message, int errorCode = 0) : FeatureSwitcherException(message, errorCode);