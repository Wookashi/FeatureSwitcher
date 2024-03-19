namespace Wookashi.FeatureSwitcher.Client.Abstraction;

public interface IFeatureManager
{
    public Task<bool> IsFeatureEnabledAsync(string featureName);
}