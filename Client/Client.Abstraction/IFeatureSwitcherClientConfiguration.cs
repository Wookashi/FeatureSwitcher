namespace Wookashi.FeatureSwitcher.Client.Abstraction;

public interface IFeatureSwitcherClientConfiguration
{
    IHttpClientFactory HttpClientFactory { get; }
    string ApplicationName { get; }
    string EnvironmentName { get; }
    List<IFeatureStateModel> Features { get; }
    Uri EnvironmentNodeAddress { get; }
}