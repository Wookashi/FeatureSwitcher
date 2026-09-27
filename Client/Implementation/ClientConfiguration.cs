using Wookashi.FeatureSwitcher.Client.Abstraction;

namespace Wookashi.FeatureSwitcher.Client.Implementation;

public sealed class FeatureSwitcherBasicClientConfiguration(
    string applicationName,
    string environmentName,
    Uri? nodeAddress,
    bool allowStartWithoutNode = false,
    TimeSpan? requestTimeout = null,
    TimeSpan? circuitBreakDuration = null)
    : IFeatureSwitcherBasicClientConfiguration
{
    public string ApplicationName { get; } = applicationName;
    public string EnvironmentName { get; } = environmentName;
    public Uri NodeAddress { get; } = nodeAddress ?? throw new ArgumentNullException(nameof(nodeAddress));
    public bool AllowStartWithoutNode { get; } = allowStartWithoutNode;
    public TimeSpan RequestTimeout { get; } = requestTimeout ?? TimeSpan.FromSeconds(3);
    public TimeSpan CircuitBreakDuration { get; } = circuitBreakDuration ?? TimeSpan.FromSeconds(30);
}
