using Wookashi.FeatureSwitcher.Node.Database.Dtos;

namespace Wookashi.FeatureSwitcher.Node.Database.Repositories;

public interface IFeatureRepository
{
    internal List<FeatureDto> GetFeaturesForApplication(string appName);
    internal void AddFeaturesForApplication(ApplicationDto application, List<FeatureDto> featuresList);
}