namespace Wookashi.FeatureSwitcher.Client.Abstraction.Exceptions;

public sealed class FeatureNameCollisionException(string message, int errorCode = 0) : ApplicationException(message, errorCode);