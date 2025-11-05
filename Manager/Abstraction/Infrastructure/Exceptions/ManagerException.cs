namespace Wookashi.FeatureSwitcher.Manager.Abstraction.Infrastructure.Exceptions;

public class ManagerException(string message, int code) : Exception(message)
{
    public int Code = code;
}