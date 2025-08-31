using Wookashi.FeatureSwitcher.Client.Abstraction;

namespace Wookashi.FeatureSwitcher.Client.Implementation;

public sealed class FeatureSwitcherBasicClientConfiguration(
    string applicationName,
    string environmentName,
    Uri? environmentNodeAddress)
    : IFeatureSwitcherBasicClientConfiguration
{
    public string ApplicationName { get; } = applicationName;
    public string EnvironmentName { get; } = environmentName;
    public Uri EnvironmentNodeAddress { get; } = environmentNodeAddress ?? throw new ArgumentNullException(nameof(environmentNodeAddress));
}