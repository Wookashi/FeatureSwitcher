namespace Wookashi.FeatureSwitcher.Client.Abstraction.Exceptions;

public sealed class EnvironmentMismatchException(string message) : ApplicationException(message, 2);