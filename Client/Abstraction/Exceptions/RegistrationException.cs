namespace Wookashi.FeatureSwitcher.Client.Abstraction.Exceptions;

/// <summary>
/// Thrown when registering the application with the node fails.
/// </summary>
public sealed class RegistrationException(string message, int errorCode = 0) : FeatureSwitcherException(message, errorCode);