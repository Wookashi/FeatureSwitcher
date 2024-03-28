using Wookashi.FeatureSwitcher.Node.Abstraction.Database.Dtos;
using Wookashi.FeatureSwitcher.Node.Abstraction.Database.Repositories;
using Wookashi.FeatureSwitcher.Node.Database.Entities;

namespace Wookashi.FeatureSwitcher.Node.Database.Repositories;

internal sealed class FeatureRepository : IFeatureRepository
{
    public List<FeatureDto> GetFeaturesForApplication(string appName)
    {
        using (var context = new FeaturesDataContext())
        {
            var list = context.Features
                .Where(feature => feature.Application.Name == appName)
                .Select(feature => new FeatureDto(
                    feature.Name,
                    feature.IsEnabled))
                .ToList();
            return list;
        }
    }

    public void AddFeaturesForApplication(ApplicationDto application, List<FeatureDto> featuresList)
    {
        if (featuresList.Count == 0)
        {
            return;
        }
        
        using (var context = new FeaturesDataContext())
        {
            var applicationEntity = context.Applications
                .FirstOrDefault(app => app.Name == application.AppName
                                       && app.Environment == application.Environment);
            if (applicationEntity is null)
            {
                context.Applications.Add(new ApplicationEntity
                {
                    Name = application.AppName,
                    Environment = application.Environment,
                });
                context.SaveChanges();
                applicationEntity = context.Applications
                    .FirstOrDefault(app => app.Name == application.AppName
                                           && app.Environment == application.Environment);
            }

            var featureEntities = featuresList.Select(feature => new FeatureEntity()
            {
                Application = applicationEntity,
                Name = feature.Name,
                IsEnabled = feature.State
            });
            context.Features.AddRange(featureEntities);
        }
    }
}