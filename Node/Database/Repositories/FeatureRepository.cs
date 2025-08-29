using Microsoft.EntityFrameworkCore;
using Wookashi.FeatureSwitcher.Node.Abstraction.Database.Dtos;
using Wookashi.FeatureSwitcher.Node.Abstraction.Database.Repositories;
using Wookashi.FeatureSwitcher.Node.Abstraction.Infrastructure.Exceptions;
using Wookashi.FeatureSwitcher.Node.Database.Entities;

namespace Wookashi.FeatureSwitcher.Node.Database.Repositories;

internal sealed class FeatureRepository : IFeatureRepository
{
    public List<ApplicationDto> GetApplications()
    {
        using (var context = new FeaturesDataContext())
        {
            return context.Applications
                .Select(application => new ApplicationDto(application.Name, application.Environment))
                .ToList();
        }
    }

    public List<FeatureDto> GetFeaturesForApplication(ApplicationDto application)
    {
        using (var context = new FeaturesDataContext())
        {
            var list = context.Features
                .Where(feature => feature.Application.Name == application.Name
                                   && feature.Application.Environment == application.Environment)
                .Select(feature => new FeatureDto(
                    feature.Name,
                    feature.IsEnabled))
                .ToList();
            return list;
        }
    }
    
    public bool GetFeatureState(ApplicationDto application, string featureName)
    {
        using (var context = new FeaturesDataContext())
        {
            var featureEntity = context.Features
                .FirstOrDefault(feature => feature.Application.Name == application.Name
                                  && feature.Application.Environment == application.Environment
                                  && feature.Name == featureName);

            if (featureEntity is null)
            {
                throw new FeatureNotFoundException("Feature not found");
            }
            return featureEntity.IsEnabled;
        }
    }

    public void UpdateFeature(ApplicationDto application, FeatureDto featureDto)
    {
        using (var context = new FeaturesDataContext())
        {
            var featureEntity = context.Features
                .FirstOrDefault(feature => feature.Application.Name == application.Name
                                           && feature.Application.Environment == application.Environment
                                           && feature.Name == featureDto.Name);

            if (featureEntity is null)
            {
                throw new FeatureNotFoundException("Feature not found");
            }
            featureEntity.IsEnabled = featureDto.State;
            context.SaveChanges();
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
                .FirstOrDefault(app => app.Name == application.Name
                                       && app.Environment == application.Environment);
            if (applicationEntity is null)
            {
                context.Applications.Add(new ApplicationEntity
                {
                    Name = application.Name,
                    Environment = application.Environment,
                });
                context.SaveChanges();
                applicationEntity = context.Applications
                    .FirstOrDefault(app => app.Name == application.Name
                                           && app.Environment == application.Environment);
            }
            
            var featureEntities = featuresList.Select(feature => new FeatureEntity
            {
                Name = feature.Name,
                IsEnabled = feature.State,
                Application = applicationEntity
            });
            context.Features.AddRange(featureEntities);
            context.SaveChanges();
        }
    }
    
    public void DeleteFeaturesForApplication(ApplicationDto application, List<FeatureDto> featuresList)
    {
        if (featuresList.Count == 0)
        {
            return;
        }
        
        using (var context = new FeaturesDataContext())
        {
            var applicationEntity = context.Applications
                .Include(applicationEntity => applicationEntity.Features)
                .FirstOrDefault(app => app.Name == application.Name
                                       && app.Environment == application.Environment);
            
            if (applicationEntity is null)
            {
                return;
            }

            foreach (var featureEntity in applicationEntity.Features)
            {
                context.Features.Remove(featureEntity);
            }
            
            context.SaveChanges();
        }
    }
}