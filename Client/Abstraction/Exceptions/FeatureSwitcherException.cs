namespace Wookashi.FeatureSwitcher.Client.Abstraction.Exceptions;

/// <summary>
/// Base exception for all Feature Switcher client errors.
/// </summary>
public class FeatureSwitcherException(string message, int code = 0) : Exception(message)
{
    /// <summary>
    /// Error code for the exception.
    /// </summary>
    public int Code { get; } = code;
}
