using Wookashi.FeatureSwitcher.Node.Database.Entities;

namespace Wookashi.FeatureSwitcher.Node.Database.Repositories;

public interface IFeatureRepository
{
    internal List<FeatureEntity> GetFeaturesForApplication(string appName);
}