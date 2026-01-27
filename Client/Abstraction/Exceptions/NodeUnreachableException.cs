namespace Wookashi.FeatureSwitcher.Client.Abstraction.Exceptions;

/// <summary>
/// Thrown when the Feature Switcher node service cannot be reached.
/// </summary>
public sealed class NodeUnreachableException(string message, int errorCode = 0) : FeatureSwitcherException(message, errorCode);