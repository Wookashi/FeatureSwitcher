using Wookashi.FeatureSwitcher.Client.Abstraction;
using Wookashi.FeatureSwitcher.Client.Abstraction.Exceptions;
using Wookashi.FeatureSwitcher.Client.Implementation.Models;

namespace Wookashi.FeatureSwitcher.Client.Implementation;

public class FeatureManager : IFeatureManager
{
    private List<FeatureStateModel> _features;
    private readonly string _appName;

    public FeatureManager(string appName, List<FeatureStateModel> features)
    {
        _appName = appName;
        _features = features;
    }

    public bool IsFeatureEnabled(string featureName)
    {
        // do the magic
        var collectionFeature = _features.FirstOrDefault(feature => feature.Name == featureName);
        if (collectionFeature is null)
        {
            throw new FeatureNotRegisteredException("Feature is not registered in node!");
        }
        return collectionFeature.IsEnabled;
    }
}