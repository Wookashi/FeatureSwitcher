using Microsoft.EntityFrameworkCore;
using Wookashi.FeatureSwitcher.Node.Abstraction.Database.Dtos;
using Wookashi.FeatureSwitcher.Node.Abstraction.Database.Repositories;
using Wookashi.FeatureSwitcher.Node.Abstraction.Infrastructure.Exceptions;
using Wookashi.FeatureSwitcher.Node.Database.Entities;

namespace Wookashi.FeatureSwitcher.Node.Database.Repositories;

internal sealed class FeatureRepository : IFeatureRepository
{
    private readonly IFeaturesDataContext _context;

    public FeatureRepository(IFeaturesDataContext dbContext)
    {
        _context = dbContext;
    }

    // ---- Read paths (Active only) ----

    public List<ApplicationDto> GetApplications()
    {
        return _context.Applications
            .Where(app => app.Status == EntityStatus.Active)
            .Select(application => new ApplicationDto(application.Name))
            .ToList();
    }

    public List<FeatureDto> GetFeaturesForApplication(ApplicationDto application)
    {
        return _context.Features
            .Where(feature => feature.Application.Name == application.Name
                              && feature.Status == EntityStatus.Active)
            .Select(feature => new FeatureDto(feature.Name, feature.IsEnabled))
            .ToList();
    }

    public List<FeatureWithUsageDto> GetFeaturesWithUsageForApplication(ApplicationDto application)
    {
        var features = _context.Features
            .Where(feature => feature.Application.Name == application.Name
                              && feature.Status == EntityStatus.Active)
            .Select(f => new { f.Id, f.Name, f.IsEnabled, f.LastUsedAt })
            .ToList();

        if (features.Count == 0)
        {
            return [];
        }

        // Last 7 days, including today. UsageDay is stored normalized to UTC midnight.
        var since = DateTime.UtcNow.Date.AddDays(-6);
        var featureIds = features.Select(f => f.Id).ToList();

        var counts = _context.FeatureUsage
            .Where(u => featureIds.Contains(u.FeatureId) && u.UsageDay >= since)
            .GroupBy(u => u.FeatureId)
            .Select(g => new { FeatureId = g.Key, Total = g.Sum(u => u.UseCount) })
            .ToList()
            .ToDictionary(x => x.FeatureId, x => x.Total);

        return features
            .Select(f => new FeatureWithUsageDto(
                f.Name,
                f.IsEnabled,
                f.LastUsedAt,
                counts.TryGetValue(f.Id, out var c) ? c : 0))
            .ToList();
    }

    public bool GetFeatureState(ApplicationDto application, string featureName)
    {
        var featureEntity = _context.Features
            .FirstOrDefault(feature => feature.Application.Name == application.Name
                                       && feature.Name == featureName
                                       && feature.Status == EntityStatus.Active);

        return featureEntity?.IsEnabled ?? throw new FeatureNotFoundException("Feature not found");
    }

    // ---- Mutations ----

    public void RegisterApplication(ApplicationDto application, List<FeatureDto> features)
    {
        var now = DateTime.UtcNow;
        var today = now.Date;

        var applicationEntity = _context.Applications
            .FirstOrDefault(app => app.Name == application.Name);

        if (applicationEntity is null)
        {
            applicationEntity = new ApplicationEntity
            {
                Name = application.Name,
                Status = EntityStatus.Active,
                LastUsedAt = now,
            };
            _context.Applications.Add(applicationEntity);
            _context.SaveChanges();
        }
        else
        {
            if (applicationEntity.Status == EntityStatus.PendingDeletion)
            {
                applicationEntity.Status = EntityStatus.Active;
                applicationEntity.PendingDeletionSince = null;
            }
            applicationEntity.LastUsedAt = now;
        }

        var existingFeatures = _context.Features
            .Where(f => f.ApplicationId == applicationEntity.Id)
            .ToList();

        foreach (var incoming in features)
        {
            var existing = existingFeatures.FirstOrDefault(f => f.Name == incoming.Name);
            if (existing is null)
            {
                var fresh = new FeatureEntity
                {
                    Name = incoming.Name,
                    IsEnabled = incoming.State,
                    Status = EntityStatus.Active,
                    LastUsedAt = now,
                    Application = applicationEntity,
                };
                _context.Features.Add(fresh);
                _context.SaveChanges();
                UpsertUsage(fresh.Id, today);
                continue;
            }

            if (existing.Status == EntityStatus.PendingDeletion)
            {
                existing.Status = EntityStatus.Active;
                existing.PendingDeletionSince = null;
            }
            existing.LastUsedAt = now;
            _context.SaveChanges();
            UpsertUsage(existing.Id, today);
        }

        _context.SaveChanges();
    }

    public void UpdateFeature(ApplicationDto application, FeatureDto featureDto)
    {
        var featureEntity = _context.Features
            .FirstOrDefault(feature => feature.Application.Name == application.Name
                                       && feature.Name == featureDto.Name
                                       && feature.Status == EntityStatus.Active);

        if (featureEntity is null)
        {
            throw new FeatureNotFoundException("Feature not found");
        }

        featureEntity.IsEnabled = featureDto.State;
        _context.SaveChanges();
    }

    public void RecordFeatureUsage(ApplicationDto application, string featureName)
    {
        var feature = _context.Features
            .Include(f => f.Application)
            .FirstOrDefault(f => f.Application.Name == application.Name && f.Name == featureName);

        if (feature is null)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var today = now.Date;

        if (feature.Status == EntityStatus.PendingDeletion)
        {
            feature.Status = EntityStatus.Active;
            feature.PendingDeletionSince = null;
        }
        feature.LastUsedAt = now;

        if (feature.Application.Status == EntityStatus.PendingDeletion)
        {
            feature.Application.Status = EntityStatus.Active;
            feature.Application.PendingDeletionSince = null;
        }
        feature.Application.LastUsedAt = now;

        _context.SaveChanges();
        UpsertUsage(feature.Id, today);
    }

    // ---- Sweep ----

    public int MarkStaleFeaturesPending(DateTime threshold)
    {
        var now = DateTime.UtcNow;
        var stale = _context.Features
            .Where(f => f.Status == EntityStatus.Active && f.LastUsedAt < threshold)
            .ToList();

        foreach (var feature in stale)
        {
            feature.Status = EntityStatus.PendingDeletion;
            feature.PendingDeletionSince = now;
        }

        if (stale.Count > 0)
        {
            _context.SaveChanges();
        }

        return stale.Count;
    }

    public int MarkStaleApplicationsPending(DateTime threshold)
    {
        var now = DateTime.UtcNow;
        var candidates = _context.Applications
            .Include(a => a.Features)
            .Where(a => a.Status == EntityStatus.Active && a.LastUsedAt < threshold)
            .ToList();

        var stale = candidates
            .Where(a => a.Features.All(f => f.Status == EntityStatus.PendingDeletion))
            .ToList();

        foreach (var app in stale)
        {
            app.Status = EntityStatus.PendingDeletion;
            app.PendingDeletionSince = now;
        }

        if (stale.Count > 0)
        {
            _context.SaveChanges();
        }

        return stale.Count;
    }

    // ---- Pending queries ----

    public List<PendingFeatureDto> GetPendingFeatures()
    {
        return _context.Features
            .Include(f => f.Application)
            .Where(f => f.Status == EntityStatus.PendingDeletion)
            .Select(f => new PendingFeatureDto(
                f.Application.Name,
                f.Name,
                f.LastUsedAt,
                f.PendingDeletionSince!.Value))
            .ToList();
    }

    public List<PendingApplicationDto> GetPendingApplications()
    {
        return _context.Applications
            .Where(a => a.Status == EntityStatus.PendingDeletion)
            .Select(a => new PendingApplicationDto(
                a.Name,
                a.LastUsedAt,
                a.PendingDeletionSince!.Value))
            .ToList();
    }

    // ---- Permanent deletion (race-protected) ----

    public DeletionResultDto PermanentlyDeleteFeature(string applicationName, string featureName)
    {
        var feature = _context.Features
            .FirstOrDefault(f => f.Application.Name == applicationName && f.Name == featureName);

        if (feature is null)
        {
            throw new FeatureNotFoundException(
                $"Feature '{featureName}' not found on application '{applicationName}'.");
        }

        if (feature.Status != EntityStatus.PendingDeletion)
        {
            throw new FeatureNotPendingDeletionException(
                $"Feature '{featureName}' on application '{applicationName}' is not in PendingDeletion state.");
        }

        var result = new DeletionResultDto(feature.LastUsedAt, feature.PendingDeletionSince!.Value);

        // FeatureUsage rows cascade via FK.
        _context.Features.Remove(feature);
        _context.SaveChanges();

        return result;
    }

    public DeletionResultDto PermanentlyDeleteApplication(string applicationName)
    {
        var application = _context.Applications
            .Include(a => a.Features)
            .FirstOrDefault(a => a.Name == applicationName);

        if (application is null)
        {
            throw new ApplicationNotFoundException(
                $"Application '{applicationName}' not found.");
        }

        if (application.Status != EntityStatus.PendingDeletion)
        {
            throw new FeatureNotPendingDeletionException(
                $"Application '{applicationName}' is not in PendingDeletion state.");
        }

        var result = new DeletionResultDto(application.LastUsedAt, application.PendingDeletionSince!.Value);

        // Features (and their usage rows) cascade via FK.
        _context.Applications.Remove(application);
        _context.SaveChanges();

        return result;
    }

    // ---- Helpers ----

    private void UpsertUsage(int featureId, DateTime usageDay)
    {
        var existing = _context.FeatureUsage
            .FirstOrDefault(u => u.FeatureId == featureId && u.UsageDay == usageDay);

        if (existing is null)
        {
            _context.FeatureUsage.Add(new FeatureUsageEntity
            {
                FeatureId = featureId,
                UsageDay = usageDay,
                UseCount = 1,
            });
        }
        else
        {
            existing.UseCount++;
        }

        _context.SaveChanges();
    }
}
