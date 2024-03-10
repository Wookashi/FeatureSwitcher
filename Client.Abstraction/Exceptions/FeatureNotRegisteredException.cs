namespace Wookashi.FeatureSwitcher.Client.Abstraction.Exceptions;

public class FeatureNotRegisteredException(string message) : ApplicationException(message, 1);