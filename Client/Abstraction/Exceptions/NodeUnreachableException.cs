namespace Wookashi.FeatureSwitcher.Client.Abstraction.Exceptions;

public sealed class NodeUnreachableException(string message, int errorCode = 0) : ApplicationException(message, errorCode);