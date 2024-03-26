using Wookashi.FeatureSwitcher.Node.Abstraction;
using Wookashi.FeatureSwitcher.Node.Database.Dtos;
using Wookashi.FeatureSwitcher.Node.Database.Repositories;
using Wookashi.FeatureSwitcher.Node.Models;

namespace Wookashi.FeatureSwitcher.Node.Services;

internal sealed class FeatureService
{
    private IFeatureRepository _featureRepository;
    private readonly string _environment;
    
    public FeatureService(IFeatureRepository featuresRepository, string environment)
    {
        _featureRepository = featuresRepository;
        _environment = environment;
     //   _environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
    }
    internal void RegisterFeatures(RegisterFeaturesRequestModel registerModel)
    {
        if (registerModel.Environment != _environment)
        {
            throw new IncorrectEnvironmentException("Environment does not match");
        }
        
        var appFeatures = _featureRepository.GetFeaturesForApplication(registerModel.AppName).ToList();

        var featuresToDelete = appFeatures
            .Where(feature => !registerModel.Features.Select(f => f.FeatureName)
                .Contains(feature.Name)).ToList();
        
        var featuresToAdd = registerModel.Features
            .Where(feature => !appFeatures.Select(ftr => ftr.Name)
                .Contains(feature.FeatureName)).ToList();

        _featureRepository.AddFeatures(featuresToAdd);
        featuresList.AddRange(featuresToAdd
            .Select(feature => new FeatureDto(
                registerModel.AppName, 
                registerModel.Environment,
                feature.FeatureName,
                feature.InitialState))
            .ToList());

        foreach (var feature in featuresToDelete)
        {
            var featureToDelete = featuresList.FirstOrDefault(ftr => ftr.FeatureName == feature.FeatureName);
            if (featureToDelete == null) continue;
            featuresList.Remove(featureToDelete);
        }
    }
    
}