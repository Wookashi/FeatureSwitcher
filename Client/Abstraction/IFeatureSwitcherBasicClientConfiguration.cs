namespace Wookashi.FeatureSwitcher.Client.Abstraction;

public interface IFeatureSwitcherBasicClientConfiguration
{
    string ApplicationName { get; }
    string EnvironmentName { get; }
    Uri NodeAddress { get; }

    /// <summary>
    /// When true, the host starts even if the Node is unreachable during startup registration.
    /// Features fall back to their initial/cached states until the Node becomes reachable.
    /// </summary>
    bool AllowStartWithoutNode { get; }
}
