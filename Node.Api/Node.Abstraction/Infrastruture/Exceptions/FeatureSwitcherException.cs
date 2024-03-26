namespace Wookashi.FeatureSwitcher.Node.Exceptions;

public class FeatureSwitcherException(string message, int code) : Exception(message)
{
    public int Code = code;
}