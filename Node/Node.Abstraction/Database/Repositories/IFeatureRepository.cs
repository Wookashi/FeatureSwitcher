using Wookashi.FeatureSwitcher.Node.Abstraction.Database.Dtos;

namespace Wookashi.FeatureSwitcher.Node.Abstraction.Database.Repositories;

public interface IFeatureRepository
{
    public List<FeatureDto> GetFeaturesForApplication(ApplicationDto application);
    public void AddFeaturesForApplication(ApplicationDto application, List<FeatureDto> featuresList);
    public void DeleteFeaturesForApplication(ApplicationDto application, List<FeatureDto> featuresList);
    public bool GetFeatureState(ApplicationDto application, string featureName);
}