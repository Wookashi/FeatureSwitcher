namespace Wookashi.FeatureSwitcher.Client.Abstraction;

public interface IFeatureManager
{
    public bool IsFeatureEnabled(string featureName);
}