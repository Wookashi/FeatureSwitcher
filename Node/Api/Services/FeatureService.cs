using Wookashi.FeatureSwitcher.Node.Abstraction.Database.Dtos;
using Wookashi.FeatureSwitcher.Node.Abstraction.Database.Repositories;
using Wookashi.FeatureSwitcher.Node.Api.Models;

namespace Wookashi.FeatureSwitcher.Node.Api.Services;

internal sealed class FeatureService
{
    private readonly IFeatureRepository _featureRepository;

    public FeatureService(IFeatureRepository featuresRepository)
    {
        _featureRepository = featuresRepository;
    }

    /// <summary>
    /// Register features for an application. Append-only: new features are created, pending ones
    /// are restored, existing active ones are left alone. Features missing from the payload are
    /// NOT deleted at registration time — a background sweep handles stale-flag cleanup.
    /// </summary>
    internal void RegisterApplication(ApplicationDto application, List<RegisterFeatureStateModel> featureModels)
    {
        var features = featureModels
            .Select(f => new FeatureDto(f.FeatureName, f.InitialState))
            .ToList();

        _featureRepository.RegisterApplication(application, features);
    }

    internal bool GetFeatureState(ApplicationDto application, string featureName)
    {
        _featureRepository.RecordFeatureUsage(application, featureName);
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

    internal List<PendingFeatureDto> GetPendingFeatures()
    {
        return _featureRepository.GetPendingFeatures();
    }

    internal List<PendingApplicationDto> GetPendingApplications()
    {
        return _featureRepository.GetPendingApplications();
    }

    internal DeletionResultDto PermanentlyDeleteFeature(string applicationName, string featureName)
    {
        return _featureRepository.PermanentlyDeleteFeature(applicationName, featureName);
    }

    internal DeletionResultDto PermanentlyDeleteApplication(string applicationName)
    {
        return _featureRepository.PermanentlyDeleteApplication(applicationName);
    }
}
