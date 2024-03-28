namespace Wookashi.FeatureSwitcher.Node.Abstraction.Infrastructure.Exceptions;

public sealed class FeatureNotFoundException(string message) : FeatureSwitcherException(message, 2);