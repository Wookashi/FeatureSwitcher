namespace Wookashi.FeatureSwitcher.Client.Abstraction.Exceptions;

/// <summary>
/// Thrown when the application's environment doesn't match the node's environment.
/// </summary>
public sealed class EnvironmentMismatchException(string message) : FeatureSwitcherException(message, 2);