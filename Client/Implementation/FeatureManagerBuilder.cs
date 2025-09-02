using Wookashi.FeatureSwitcher.Client.Abstraction;
using Wookashi.FeatureSwitcher.Client.Abstraction.Exceptions;

namespace Wookashi.FeatureSwitcher.Client.Implementation;

public sealed class FeatureManagerBuilder
{
    private readonly IFeatureSwitcherBasicClientConfiguration _basicConfiguration;
    private IHttpClientFactory _httpClientFactory;
    private List<IFeatureStateModel> _features;
    
    public FeatureManagerBuilder(IFeatureSwitcherBasicClientConfiguration clientBasicConfiguration)
    {
        if (clientBasicConfiguration is null)
        {
            throw new ArgumentNullException(nameof(clientBasicConfiguration));
        }
        if (string.IsNullOrWhiteSpace(clientBasicConfiguration.ApplicationName))
        {
            throw new ArgumentNullException(nameof(clientBasicConfiguration.ApplicationName));
        }
        if (string.IsNullOrWhiteSpace(clientBasicConfiguration.EnvironmentName))
        {
            throw new ArgumentNullException(nameof(clientBasicConfiguration.EnvironmentName));
        }
        if (clientBasicConfiguration.EnvironmentNodeAddress is null)
        {
            throw new ArgumentNullException(nameof(clientBasicConfiguration.EnvironmentNodeAddress));
        }
        _basicConfiguration = clientBasicConfiguration;
    }
    
    public FeatureManagerBuilder AddFeatures(List<IFeatureStateModel> features)
    {
        if (features is null)
        {
            throw new ArgumentNullException(nameof(features));
        }
        if (features
            .GroupBy(feature => feature.Name)
            .Any(g => g.Count() > 1))
        {
            throw new FeatureNameCollisionException("Feature names must be unique!");
        }
        _features = features;
        return this;
    }
    
    public FeatureManagerBuilder AddHttpClientFactory(IHttpClientFactory clientFactory)
    {
        if (clientFactory is null)
        {
            throw new ArgumentNullException(nameof(clientFactory));
        }
        _httpClientFactory = clientFactory;
        return this;
    }

    private FeatureSwitcherFullClientConfiguration PrepareFullConfiguration()
    {
        return new FeatureSwitcherFullClientConfiguration(
            applicationName: _basicConfiguration.ApplicationName,
            environmentName: _basicConfiguration.EnvironmentName,
            environmentNodeAddress: _basicConfiguration.EnvironmentNodeAddress,
            features: _features,
            httpClientFactory: _httpClientFactory);
    }
    
    public async Task<FeatureManager> BuildAsync()
    {
        //TODO check configuration
        var fullConfig = PrepareFullConfiguration();
        var featureManager = new FeatureManager(fullConfig);
        await featureManager.RegisterFeaturesOnNode();
        return featureManager;
    }
}