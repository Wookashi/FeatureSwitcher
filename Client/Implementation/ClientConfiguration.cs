using Wookashi.FeatureSwitcher.Client.Abstraction;

namespace Wookashi.FeatureSwitcher.Client.Implementation;

public sealed class FeatureSwitcherBasicClientConfiguration(
    string applicationName,
    string environmentName,
    Uri? nodeAddress)
    : IFeatureSwitcherBasicClientConfiguration
{
    public string ApplicationName { get; } = applicationName;
    public string EnvironmentName { get; } = environmentName;
    public Uri NodeAddress { get; } = nodeAddress ?? throw new ArgumentNullException(nameof(nodeAddress));
}