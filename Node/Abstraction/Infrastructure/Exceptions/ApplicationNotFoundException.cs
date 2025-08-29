namespace Wookashi.FeatureSwitcher.Node.Abstraction.Infrastructure.Exceptions;

public sealed class ApplicationNotFoundException(string message) : FeatureSwitcherException(message, 3);