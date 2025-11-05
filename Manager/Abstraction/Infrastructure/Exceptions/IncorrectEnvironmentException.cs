namespace Wookashi.FeatureSwitcher.Manager.Abstraction.Infrastructure.Exceptions;

public sealed class IncorrectEnvironmentException(string message) : ManagerException(message, 1);