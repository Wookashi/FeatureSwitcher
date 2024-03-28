namespace Wookashi.FeatureSwitcher.Client.Abstraction.Exceptions;

public class ApplicationException(string message, int code) : Exception(message)
{
    public int Code = code;
}