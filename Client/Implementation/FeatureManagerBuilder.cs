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
        if (_basicConfiguration is null)
        {
            throw new ArgumentNullException(nameof(_basicConfiguration));
        }
        if (string.IsNullOrWhiteSpace(_basicConfiguration.ApplicationName))
        {
            throw new ArgumentNullException(nameof(_basicConfiguration.ApplicationName));
        }
        if (string.IsNullOrWhiteSpace(_basicConfiguration.EnvironmentName))
        {
            throw new ArgumentNullException(nameof(_basicConfiguration.EnvironmentName));
        }
        if (_basicConfiguration.EnvironmentNodeAddress is null)
        {
            throw new ArgumentNullException(nameof(_basicConfiguration.EnvironmentNodeAddress));
        }
        _basicConfiguration = clientBasicConfiguration;
    }
    
    public FeatureManagerBuilder AddFeatures(List<IFeatureStateModel> features)
    {
        if (_features is null)
        {
            throw new ArgumentNullException(nameof(_features));
        }
        if (_features
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
        if (_httpClientFactory is null)
        {
            throw new ArgumentNullException(nameof(_httpClientFactory));
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