using Wookashi.FeatureSwitcher.Abstraction.Models;

namespace Wookashi.FeatureSwitcher.Abstraction;

public interface IFeatureManager
{
    public void RegisterFeatures(List<FeatureStateModel> features);
    public bool IsFeatureEnabled(string featureName);
}