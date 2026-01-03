

using Wookashi.FeatureSwitcher.Manager.Abstraction.Database.Dtos;

namespace Wookashi.FeatureSwitcher.Manager.Abstraction.Database.Repositories;

public interface INodeRepository
{
    public void CreateOrUpdateNode(NodeDto nodeDto);
    public void UpdateFeatureState();
    
    //
    // public List<ApplicationDto> GetApplications();
    // public List<FeatureDto> GetFeaturesForApplication(ApplicationDto application);
    // public void AddFeaturesForApplication(ApplicationDto application, List<FeatureDto> featuresList);
    // public void DeleteFeaturesForApplication(ApplicationDto application, List<FeatureDto> featuresList);
    // public bool GetFeatureState(ApplicationDto application, string featureName);
    // public void UpdateFeature(ApplicationDto application, FeatureDto featureDto);
}