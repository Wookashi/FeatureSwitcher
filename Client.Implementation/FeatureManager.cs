using Wookashi.FeatureSwitcher.Client.Abstraction;
using Wookashi.FeatureSwitcher.Client.Abstraction.Exceptions;
using Wookashi.FeatureSwitcher.Client.Implementation.Models;

namespace Wookashi.FeatureSwitcher.Client.Implementation;

public class FeatureManager : IFeatureManager
{
    private List<FeatureStateModel> _features;
    private readonly string _appName;
    private IHttpClientFactory _httpClientFactory;
    private Uri _nodeAddress = new Uri("http://localhost:5216");

    public FeatureManager(string appName, List<FeatureStateModel> features, Uri? nodeAddress = null)
    {
        _appName = appName;
        _features = features;
        if (nodeAddress is not null)
        {
            _nodeAddress = nodeAddress;
        }
    }

    public bool IsFeatureEnabled(string featureName)
    {
        var collectionFeature = _features.FirstOrDefault(feature => feature.Name == featureName);
        if (collectionFeature is null)
        {
            throw new FeatureNotRegisteredException("Feature is not registered in node!");
        }
        var nodeState = 
        return collectionFeature.IsEnabled;
    }
}