using Wookashi.FeatureSwitcher.Node.Abstraction.Database.Dtos;

namespace Wookashi.FeatureSwitcher.Node.Abstraction.Database.Repositories;

public interface IFeatureRepository
{
    internal List<FeatureDto> GetFeaturesForApplication(string appName);
    internal void AddFeaturesForApplication(ApplicationDto application, List<FeatureDto> featuresList);
}