using Wookashi.FeatureSwitcher.Client.Abstraction;

namespace Wookashi.FeatureSwitcher.Client.Implementation;

internal sealed class FeatureSwitcherFullClientConfiguration(
    IHttpClientFactory httpClientFactory,
    string applicationName,
    string environmentName,
    List<IFeatureStateModel> features,
    Uri? environmentNodeAddress)
{
    public IHttpClientFactory HttpClientFactory { get; } = httpClientFactory;
    public string ApplicationName { get;} = applicationName;
    public string EnvironmentName { get; } = environmentName;
    public List<IFeatureStateModel> Features { get; } = features;
    public Uri EnvironmentNodeAddress { get; } = environmentNodeAddress ?? throw new ArgumentNullException(nameof(environmentNodeAddress));
}