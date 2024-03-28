namespace Wookashi.FeatureSwitcher.Node.Abstraction.Infrastructure.Exceptions;

public sealed class IncorrectEnvironmentException(string message) : FeatureSwitcherException(message, 1);