namespace Wookashi.FeatureSwitcher.Client.Abstraction.Exceptions;

public sealed class RegistrationException(string message, int errorCode = 0) : ApplicationException(message, errorCode);