using System.Collections.Generic;
using Wookashi.FeatureSwitcher.Abstraction;
using Wookashi.FeatureSwitcher.Abstraction.Models;

namespace Wookashi.FeatureSwitcher.Client;

public class FeatureManager : IFeatureManager
{
    private bool _alreadyRegistered;
    public void RegisterFeatures(List<FeatureStateModel> features)
    {
        _alreadyRegistered = true;
     //   throw new System.NotImplementedException();
    }

    public bool IsFeatureEnabled(string featureName)
    {
        return true;
    }
}