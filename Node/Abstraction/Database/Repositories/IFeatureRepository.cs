using Wookashi.FeatureSwitcher.Node.Abstraction.Database.Dtos;

namespace Wookashi.FeatureSwitcher.Node.Abstraction.Database.Repositories;

public interface IFeatureRepository
{
    // ---- Read paths (Active only by default) ----

    public List<ApplicationDto> GetApplications();
    public List<FeatureDto> GetFeaturesForApplication(ApplicationDto application);

    /// <summary>
    /// Same as <see cref="GetFeaturesForApplication"/> but each feature carries usage metadata
    /// (LastUsedAt and the aggregated use count for the last 7 days). Used by the Manager UI.
    /// </summary>
    public List<FeatureWithUsageDto> GetFeaturesWithUsageForApplication(ApplicationDto application);

    public bool GetFeatureState(ApplicationDto application, string featureName);

    // ---- Mutations ----

    /// <summary>
    /// Append-only: adds features that don't exist, restores any in PendingDeletion that are in
    /// the payload. Existing Active features are left as-is. Bumps application LastUsedAt and
    /// restores the application if it was PendingDeletion.
    /// </summary>
    public void RegisterApplication(ApplicationDto application, List<FeatureDto> features);

    public void UpdateFeature(ApplicationDto application, FeatureDto featureDto);

    /// <summary>
    /// Records a single use for the given (app, feature) pair on today's UTC date. If the feature
    /// is PendingDeletion, it is also restored. Bumps LastUsedAt on both feature and application.
    /// </summary>
    public void RecordFeatureUsage(ApplicationDto application, string featureName);

    // ---- Sweep ----

    /// <summary>
    /// Marks every Active feature whose LastUsedAt is older than the threshold as PendingDeletion.
    /// Returns the number transitioned.
    /// </summary>
    public int MarkStaleFeaturesPending(DateTime threshold);

    /// <summary>
    /// Marks every Active application as PendingDeletion when (a) it has no Active features and
    /// (b) its LastUsedAt is older than the threshold. Returns the number transitioned.
    /// </summary>
    public int MarkStaleApplicationsPending(DateTime threshold);

    // ---- Pending-delete queries ----

    public List<PendingFeatureDto> GetPendingFeatures();
    public List<PendingApplicationDto> GetPendingApplications();

    // ---- Permanent deletion (race-protected) ----

    /// <summary>
    /// Permanently removes a feature and all its usage rows. Throws
    /// <see cref="Wookashi.FeatureSwitcher.Node.Abstraction.Infrastructure.Exceptions.FeatureNotPendingDeletionException"/>
    /// if the feature is not currently in PendingDeletion state (it was restored by a recent use).
    /// </summary>
    public DeletionResultDto PermanentlyDeleteFeature(string applicationName, string featureName);

    /// <summary>
    /// Permanently removes an application and all its features and usage rows. Same race-protection
    /// behavior as <see cref="PermanentlyDeleteFeature"/>.
    /// </summary>
    public DeletionResultDto PermanentlyDeleteApplication(string applicationName);
}
