namespace Wookashi.FeatureSwitcher.Node.Abstraction.Infrastructure.Exceptions;

public class FeatureSwitcherException(string message, int code) : Exception(message)
{
    public int Code = code;
}