using Microsoft.EntityFrameworkCore;
using Wookashi.FeatureSwitcher.Node.Database.Entities;

namespace Wookashi.FeatureSwitcher.Node.Database.Repositories;

internal sealed class FeatureRepository
{
    public List<FeatureEntity> GetFeaturesForApplication(string appName)
    {
        using (var context = new FeaturesDataContext())
        {
            var list = context.Features
                .Where(feature => feature.Application.Name == appName)
                .Include(featureEntity => featureEntity.Application)
                .ToList();
            return list;
        }
    }
}