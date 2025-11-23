

namespace Wookashi.FeatureSwitcher.Manager.Abstraction.Database.Repositories;

public interface IFeatureStatesRepository
{
    public void NodeRegistration();
    public void UpdateFeatureState();
    
    //
    // public List<ApplicationDto> GetApplications();
    // public List<FeatureDto> GetFeaturesForApplication(ApplicationDto application);
    // public void AddFeaturesForApplication(ApplicationDto application, List<FeatureDto> featuresList);
    // public void DeleteFeaturesForApplication(ApplicationDto application, List<FeatureDto> featuresList);
    // public bool GetFeatureState(ApplicationDto application, string featureName);
    // public void UpdateFeature(ApplicationDto application, FeatureDto featureDto);
}