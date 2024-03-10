using Wookashi.FeatureSwitcher.Client.Abstraction;
using Wookashi.FeatureSwitcher.Client.Implementation.Models;

namespace Wookashi.FeatureSwitcher.Client.Implementation;

public class FeatureManager : IFeatureManager
{
    private bool _alreadyRegistered;
    public void RegisterFeatures(string appName, List<FeatureStateModel> features)
    {
        if (_alreadyRegistered)
        {
            throw new InvalidOperationException("Features was registered already!");
        }
        _alreadyRegistered = true;
        
     //   throw new System.NotImplementedException();
    }

    public bool IsFeatureEnabled(string featureName)
    {
        return true;
    }
}