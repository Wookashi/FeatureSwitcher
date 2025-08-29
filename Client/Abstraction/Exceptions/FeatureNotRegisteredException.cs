namespace Wookashi.FeatureSwitcher.Client.Abstraction.Exceptions;

public sealed class FeatureNotRegisteredException(string message) : ApplicationException(message, 1);