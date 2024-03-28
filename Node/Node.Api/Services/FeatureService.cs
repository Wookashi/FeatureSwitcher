using Wookashi.FeatureSwitcher.Node.Abstraction.Database.Dtos;
using Wookashi.FeatureSwitcher.Node.Abstraction.Database.Repositories;
using Wookashi.FeatureSwitcher.Node.Abstraction.Infrastructure.Exceptions;
using Wookashi.FeatureSwitcher.Node.Api.Models;

namespace Wookashi.FeatureSwitcher.Node.Api.Services;

internal sealed class FeatureService
{
    private readonly IFeatureRepository _featureRepository;
    private readonly ApplicationDto _application;
    
    public FeatureService(IFeatureRepository featuresRepository, ApplicationDto application)
    {
        _featureRepository = featuresRepository;
        _application = application;
     //   _environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
    }

    /// <summary>
    /// Register features for application. new features will be added, current will stay and unsupported will be deleted
    /// </summary>
    /// <param name="featureModels"></param>
    /// <exception cref="IncorrectEnvironmentException"></exception>
    internal void RegisterFeatures(List<RegisterFeatureStateModel> featureModels)
    {
        var appFeatures = _featureRepository.GetFeaturesForApplication(_application).ToList();

        var featuresToAdd = featureModels
            .Where(feature => !appFeatures
            .Select(ftr => ftr.Name)
            .Contains(feature.FeatureName))
            .Select(feature => new FeatureDto(feature.FeatureName, feature.InitialState))
            .ToList();
        
        if (featuresToAdd.Any())
        {
            _featureRepository.AddFeaturesForApplication(_application, featuresToAdd);
        }

        
        var featuresToDelete = appFeatures
            .Where(feature => !featureModels.Select(f => f.FeatureName)
                .Contains(feature.Name)).ToList();
        
        if (featuresToDelete.Any())
        {
            _featureRepository.DeleteFeaturesForApplication(_application, featuresToDelete);
        }
    }

    internal bool GetFeatureState(string featureName)
    {
        return _featureRepository.GetFeatureState(_application, featureName);
    }
}