namespace Wookashi.FeatureSwitcher.Client.Abstraction;

public interface IFeatureManager
{
    public void RegisterFeatures(List<FeatureStateModel> features);
    public bool IsFeatureEnabled(string featureName);
}