namespace Wookashi.FeatureSwitcher.Node.Abstraction.Infrastruture.Exceptions;

public class FeatureSwitcherException(string message, int code) : Exception(message)
{
    public int Code = code;
}