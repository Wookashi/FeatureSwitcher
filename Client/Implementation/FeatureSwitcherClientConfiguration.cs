using Wookashi.FeatureSwitcher.Client.Abstraction;

namespace Wookashi.FeatureSwitcher.Client.Implementation;

public sealed class FeatureSwitcherClientConfiguration(
    IHttpClientFactory httpClientFactory,
    string applicationName,
    string environmentName,
    List<IFeatureStateModel> features,
    Uri? environmentNodeAddress)
    : IFeatureSwitcherClientConfiguration
{
    public IHttpClientFactory HttpClientFactory { get; } = httpClientFactory;
    public string ApplicationName { get; } = applicationName;
    public string EnvironmentName { get; } = environmentName;
    public List<IFeatureStateModel> Features { get; } = features;
    public Uri EnvironmentNodeAddress { get; } = environmentNodeAddress ?? throw new ArgumentNullException(nameof(environmentNodeAddress));
}