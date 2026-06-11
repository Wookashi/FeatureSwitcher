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
        return _context.ApplicationFeatures
            .Where(link => link.Application.Name == application.Name
                           && link.Status == EntityStatus.Active
                           && link.Feature.Status == EntityStatus.Active)
            .Select(link => new FeatureDto(link.Feature.Name, link.Feature.IsEnabled))
            .ToList();
    }

    public List<FeatureWithUsageDto> GetFeaturesWithUsageForApplication(ApplicationDto application)
    {
        var features = _context.ApplicationFeatures
            .Where(link => link.Application.Name == application.Name
                           && link.Status == EntityStatus.Active
                           && link.Feature.Status == EntityStatus.Active)
            .Select(link => new
            {
                link.Feature.Id,
                link.Feature.Name,
                link.Feature.IsEnabled,
                link.LastUsedAt
            })
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
        var featureEntity = FindFeatureForApplication(application.Name, featureName, EntityStatus.Active);

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

        var existingLinks = _context.ApplicationFeatures
            .Include(link => link.Feature)
            .Where(link => link.ApplicationId == applicationEntity.Id)
            .ToList();

        foreach (var incoming in features)
        {
            var existingLink = existingLinks.FirstOrDefault(link => link.Feature.Name == incoming.Name);
            var existing = existingLink?.Feature;

            if (existing is null)
            {
                existing = _context.Features
                    .FirstOrDefault(f => f.Name == incoming.Name);

                if (existing is null)
                {
                    existing = new FeatureEntity
                    {
                        Name = incoming.Name,
                        IsEnabled = incoming.State,
                        Status = EntityStatus.Active,
                        LastUsedAt = now,
                    };
                    _context.Features.Add(existing);
                    _context.SaveChanges();
                }

                _context.ApplicationFeatures.Add(new ApplicationFeatureEntity
                {
                    ApplicationId = applicationEntity.Id,
                    FeatureId = existing.Id,
                    Status = EntityStatus.Active,
                    LastUsedAt = now,
                });
                _context.SaveChanges();
            }
            else
            {
                if (existingLink!.Status == EntityStatus.PendingDeletion)
                {
                    existingLink.Status = EntityStatus.Active;
                    existingLink.PendingDeletionSince = null;
                }
                existingLink.LastUsedAt = now;
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

    public FeatureUpdateResultDto UpdateFeature(ApplicationDto application, FeatureDto featureDto)
    {
        var featureEntity = FindFeatureForApplication(application.Name, featureDto.Name, EntityStatus.Active);

        if (featureEntity is null)
        {
            throw new FeatureNotFoundException("Feature not found");
        }

        featureEntity.IsEnabled = featureDto.State;
        var affectedApplications = _context.ApplicationFeatures
            .Count(link => link.FeatureId == featureEntity.Id
                           && link.Status == EntityStatus.Active
                           && link.Application.Status == EntityStatus.Active);

        _context.SaveChanges();

        return new FeatureUpdateResultDto(featureDto.Name, featureDto.State, affectedApplications);
    }

    public void RecordFeatureUsage(ApplicationDto application, string featureName)
    {
        var link = _context.ApplicationFeatures
            .Include(l => l.Application)
            .Include(l => l.Feature)
            .FirstOrDefault(l => l.Application.Name == application.Name
                                 && l.Feature.Name == featureName);

        if (link is null)
        {
            return;
        }

        var feature = link.Feature;
        var applicationEntity = link.Application;
        var now = DateTime.UtcNow;
        var today = now.Date;

        if (link.Status == EntityStatus.PendingDeletion)
        {
            link.Status = EntityStatus.Active;
            link.PendingDeletionSince = null;
        }
        link.LastUsedAt = now;

        if (feature.Status == EntityStatus.PendingDeletion)
        {
            feature.Status = EntityStatus.Active;
            feature.PendingDeletionSince = null;
        }
        feature.LastUsedAt = now;

        if (applicationEntity.Status == EntityStatus.PendingDeletion)
        {
            applicationEntity.Status = EntityStatus.Active;
            applicationEntity.PendingDeletionSince = null;
        }
        applicationEntity.LastUsedAt = now;

        _context.SaveChanges();
        UpsertUsage(feature.Id, today);
    }

    // ---- Sweep ----

    public int MarkStaleApplicationFeaturesPending(DateTime threshold)
    {
        var now = DateTime.UtcNow;
        var stale = _context.ApplicationFeatures
            .Where(link => link.Status == EntityStatus.Active && link.LastUsedAt < threshold)
            .ToList();

        foreach (var link in stale)
        {
            link.Status = EntityStatus.PendingDeletion;
            link.PendingDeletionSince = now;
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
            .Include(a => a.ApplicationFeatures)
            .ThenInclude(link => link.Feature)
            .Where(a => a.Status == EntityStatus.Active && a.LastUsedAt < threshold)
            .ToList();

        var stale = candidates
            .Where(a => a.ApplicationFeatures.All(link => link.Status == EntityStatus.PendingDeletion))
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
        return _context.ApplicationFeatures
            .Where(link => link.Status == EntityStatus.PendingDeletion)
            .Select(link => new PendingFeatureDto(
                link.Application.Name,
                link.Feature.Name,
                link.LastUsedAt,
                link.PendingDeletionSince!.Value))
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
        var link = _context.ApplicationFeatures
            .Include(applicationFeature => applicationFeature.Feature)
            .FirstOrDefault(applicationFeature => applicationFeature.Application.Name == applicationName
                                                  && applicationFeature.Feature.Name == featureName);

        if (link is null)
        {
            throw new FeatureNotFoundException(
                $"Feature '{featureName}' not found on application '{applicationName}'.");
        }

        if (link.Status != EntityStatus.PendingDeletion)
        {
            throw new FeatureNotPendingDeletionException(
                $"Feature '{featureName}' on application '{applicationName}' is not in PendingDeletion state.");
        }

        var hasOtherApplications = _context.ApplicationFeatures
            .Any(other => other.FeatureId == link.FeatureId && other.ApplicationId != link.ApplicationId);
        var deletedFeatures = hasOtherApplications ? 0 : 1;
        var result = new DeletionResultDto(
            link.LastUsedAt,
            link.PendingDeletionSince!.Value,
            removedApplicationFeatureLinks: 1,
            deletedFeatures);

        _context.ApplicationFeatures.Remove(link);

        if (!hasOtherApplications)
        {
            _context.Features.Remove(link.Feature);
        }

        _context.SaveChanges();

        return result;
    }

    public DeletionResultDto PermanentlyDeleteApplication(string applicationName)
    {
        var application = _context.Applications
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

        var links = _context.ApplicationFeatures
            .Include(link => link.Feature)
            .Where(link => link.ApplicationId == application.Id)
            .ToList();
        var deletedFeatures = 0;

        foreach (var link in links)
        {
            var hasOtherApplications = _context.ApplicationFeatures
                .Any(other => other.FeatureId == link.FeatureId && other.ApplicationId != application.Id);

            _context.ApplicationFeatures.Remove(link);

            if (!hasOtherApplications)
            {
                deletedFeatures++;
                _context.Features.Remove(link.Feature);
            }
        }

        var result = new DeletionResultDto(
            application.LastUsedAt,
            application.PendingDeletionSince!.Value,
            removedApplicationFeatureLinks: links.Count,
            deletedFeatures);

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

    private FeatureEntity? FindFeatureForApplication(
        string applicationName,
        string featureName,
        EntityStatus? requiredStatus = null)
    {
        var query = _context.ApplicationFeatures
            .Where(link => link.Application.Name == applicationName
                           && link.Feature.Name == featureName
                           && link.Status == EntityStatus.Active);

        if (requiredStatus is not null)
        {
            query = query.Where(link => link.Feature.Status == requiredStatus);
        }

        return query
            .Select(link => link.Feature)
            .FirstOrDefault();
    }
}
