using Wookashi.FeatureSwitcher.Node.Abstraction.Database.Dtos;
using Wookashi.FeatureSwitcher.Node.Abstraction.Database.Repositories;
using Wookashi.FeatureSwitcher.Node.Api.Models;
using Wookashi.FeatureSwitcher.Node.Database.Repositories;

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
    /// Feature details are brought within the limits instead of rejecting the registration; the
    /// adjustments are returned per feature so the caller can log them.
    /// </summary>
    internal IReadOnlyList<(string FeatureName, IReadOnlyList<string> Problems)> RegisterApplication(
        ApplicationDto application, List<RegisterFeatureStateModel> featureModels)
    {
        var adjustments = new List<(string, IReadOnlyList<string>)>();
        var features = featureModels
            .Select(f =>
            {
                var problems = new List<string>();
                var details = FeatureDetailsSanitizer.Sanitize(
                    new FeatureDetailsDto(
                        f.Description,
                        f.Links?.Select(l => new FeatureLinkDto(l?.Url!, l?.Title)).ToList() ?? []),
                    problems);
                if (problems.Count > 0)
                {
                    adjustments.Add((f.FeatureName, problems));
                }

                return new FeatureDto(f.FeatureName, f.InitialState) { Details = details };
            })
            .ToList();

        _featureRepository.RegisterApplication(application, features);
        return adjustments;
    }

    internal bool GetFeatureState(ApplicationDto application, string featureName)
    {
        _featureRepository.RecordFeatureUsage(application, featureName);
        return _featureRepository.GetFeatureState(application, featureName);
    }

    internal FeatureUpdateResultDto UpdateFeature(ApplicationDto application, FeatureDto feature)
    {
        return _featureRepository.UpdateFeature(application, feature);
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
