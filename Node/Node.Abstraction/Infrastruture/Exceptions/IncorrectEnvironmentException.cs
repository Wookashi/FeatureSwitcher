namespace Wookashi.FeatureSwitcher.Node.Abstraction.Infrastruture.Exceptions;

public sealed class IncorrectEnvironmentException(string message) : FeatureSwitcherException(message, 1);