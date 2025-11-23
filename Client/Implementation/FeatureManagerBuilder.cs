using Wookashi.FeatureSwitcher.Client.Abstraction;
using Wookashi.FeatureSwitcher.Client.Abstraction.Exceptions;

namespace Wookashi.FeatureSwitcher.Client.Implementation;

public sealed class FeatureManagerBuilder
{
    private readonly IFeatureSwitcherBasicClientConfiguration _basicConfiguration;
    private IHttpClientFactory? _httpClientFactory;
    private List<IFeatureStateModel>? _features;

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
        _httpClientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
        return this;
    }

    private FeatureSwitcherFullClientConfiguration PrepareFullConfiguration()
    {
        return new FeatureSwitcherFullClientConfiguration(
            applicationName: _basicConfiguration.ApplicationName,
            environmentName: _basicConfiguration.EnvironmentName,
            environmentNodeAddress: _basicConfiguration.EnvironmentNodeAddress,
            features: _features ?? throw new InvalidOperationException(),
            httpClientFactory: _httpClientFactory ?? throw new InvalidOperationException());
    }

    private bool ValidateProperties()
    {
        if (_features is null)
        {
            throw new MissingMemberException("Features must be set before build object");
        }

        if (_httpClientFactory is null)
        {
            throw new MissingMemberException("Http client factory must be set before build object");
        }
        return true;
    }

    public async Task<FeatureManager> BuildAsync()
    {
        if (ValidateProperties())
        {
            var fullConfig = PrepareFullConfiguration();
            var featureManager = new FeatureManager(fullConfig);
            await featureManager.RegisterFeaturesOnNodeAsync();
            return featureManager;
        }
        throw new Exception("Cannot build object!");
    }
}