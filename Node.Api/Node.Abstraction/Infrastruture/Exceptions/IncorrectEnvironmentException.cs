using Wookashi.FeatureSwitcher.Node.Exceptions;

namespace Wookashi.FeatureSwitcher.Node.Abstraction;

public sealed class IncorrectEnvironmentException(string message) : FeatureSwitcherException(message, 1);