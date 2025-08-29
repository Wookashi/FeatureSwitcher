using Wookashi.FeatureSwitcher.Node.Abstraction.Database.Dtos;
using Wookashi.FeatureSwitcher.Node.Abstraction.Database.Repositories;
using Wookashi.FeatureSwitcher.Node.Abstraction.Infrastructure.Exceptions;
using Wookashi.FeatureSwitcher.Node.Api.Models;

namespace Wookashi.FeatureSwitcher.Node.Api.Services;

internal sealed class FeatureService
{
    private readonly IFeatureRepository _featureRepository;
    
    public FeatureService(IFeatureRepository featuresRepository)
    {
        _featureRepository = featuresRepository;
     //   _environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
    }

    /// <summary>
    /// Register features for application. new features will be added, current will stay and unsupported will be deleted
    /// </summary>
    /// <param name="application">Application to register</param>
    /// <param name="featureModels">Features to register</param>
    /// <exception cref="IncorrectEnvironmentException"></exception>
    internal void RegisterApplication(ApplicationDto application, List<RegisterFeatureStateModel> featureModels)
    {
        var appFeatures = _featureRepository.GetFeaturesForApplication(application).ToList();

        var featuresToAdd = featureModels
            .Where(feature => !appFeatures
            .Select(ftr => ftr.Name)
            .Contains(feature.FeatureName))
            .Select(feature => new FeatureDto(feature.FeatureName, feature.InitialState))
            .ToList();
        
        if (featuresToAdd.Any())
        {
            _featureRepository.AddFeaturesForApplication(application, featuresToAdd);
        }

        
        var featuresToDelete = appFeatures
            .Where(feature => !featureModels.Select(f => f.FeatureName)
                .Contains(feature.Name)).ToList();
        
        if (featuresToDelete.Any())
        {
            _featureRepository.DeleteFeaturesForApplication(application, featuresToDelete);
        }
    }

    internal bool GetFeatureState(ApplicationDto application, string featureName)
    {
        return _featureRepository.GetFeatureState(application, featureName);
    }
    
    internal void UpdateFeature(ApplicationDto application, FeatureDto feature)
    {
        _featureRepository.UpdateFeature(application, feature);
    }
    
    internal List<ApplicationDto> GetApplications()
    {
        return _featureRepository.GetApplications();
    }
    
    internal List<FeatureDto> GetFeaturesForApplication(ApplicationDto application)
    {
        return _featureRepository.GetFeaturesForApplication(application);
    }
}