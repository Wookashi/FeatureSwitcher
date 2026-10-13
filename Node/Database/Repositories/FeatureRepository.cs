using Microsoft.EntityFrameworkCore;
using Wookashi.FeatureSwitcher.Node.Abstraction.Database.Dtos;
using Wookashi.FeatureSwitcher.Node.Abstraction.Database.Repositories;
using Wookashi.FeatureSwitcher.Node.Abstraction.Infrastructure.Exceptions;
using Wookashi.FeatureSwitcher.Node.Database.Entities;

namespace Wookashi.FeatureSwitcher.Node.Database.Repositories;

internal sealed class FeatureRepository : IFeatureRepository
{
    private readonly IFeaturesDataContext _context;
    private readonly FeatureUsageBuffer _usageBuffer;

    public FeatureRepository(IFeaturesDataContext dbContext, FeatureUsageBuffer usageBuffer)
    {
        _context = dbContext;
        _usageBuffer = usageBuffer;
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
            ApplicationFeatureEntity link;

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

                link = new ApplicationFeatureEntity
                {
                    ApplicationId = applicationEntity.Id,
                    FeatureId = existing.Id,
                    Status = EntityStatus.Active,
                    LastUsedAt = now,
                };
                _context.ApplicationFeatures.Add(link);
                _context.SaveChanges();
            }
            else
            {
                link = existingLink!;
                if (link.Status == EntityStatus.PendingDeletion)
                {
                    link.Status = EntityStatus.Active;
                    link.PendingDeletionSince = null;
                }
                link.LastUsedAt = now;
            }

            if (existing.Status == EntityStatus.PendingDeletion)
            {
                existing.Status = EntityStatus.Active;
                existing.PendingDeletionSince = null;
            }
            existing.LastUsedAt = now;
            _usageBuffer.Record(link.Id, applicationEntity.Id, existing.Id, now);
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
        var affectedApplicationNames = _context.ApplicationFeatures
            .Where(link => link.FeatureId == featureEntity.Id
                           && link.Status == EntityStatus.Active
                           && link.Application.Status == EntityStatus.Active)
            .Select(link => link.Application.Name)
            .OrderBy(name => name)
            .ToList();

        _context.SaveChanges();

        return new FeatureUpdateResultDto(featureDto.Name, featureDto.State, affectedApplicationNames);
    }

    public void RecordFeatureUsage(ApplicationDto application, string featureName)
    {
        // Hot path (every flag-state read): a single projection query, no write. Only a link that
        // has to be restored from PendingDeletion is written synchronously, so the state read that
        // follows sees it as Active.
        var usage = _context.ApplicationFeatures
            .Where(l => l.Application.Name == application.Name && l.Feature.Name == featureName)
            .Select(l => new
            {
                l.Id,
                l.ApplicationId,
                l.FeatureId,
                NeedsRestore = l.Status == EntityStatus.PendingDeletion
                               || l.Feature.Status == EntityStatus.PendingDeletion
                               || l.Application.Status == EntityStatus.PendingDeletion,
            })
            .FirstOrDefault();

        if (usage is null)
        {
            return;
        }

        var now = DateTime.UtcNow;

        if (usage.NeedsRestore)
        {
            RestoreLink(usage.Id, now);
        }

        _usageBuffer.Record(usage.Id, usage.ApplicationId, usage.FeatureId, now);
    }

    public int FlushFeatureUsage()
    {
        lock (_usageBuffer.FlushLock)
        {
            var entries = _usageBuffer.Drain();
            if (entries.Count == 0)
            {
                return 0;
            }

            try
            {
                WriteUsage(entries);
            }
            catch
            {
                _usageBuffer.Requeue(entries);
                throw;
            }

            return entries.Count;
        }
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

    private void RestoreLink(int applicationFeatureId, DateTime now)
    {
        var link = _context.ApplicationFeatures
            .Include(l => l.Application)
            .Include(l => l.Feature)
            .FirstOrDefault(l => l.Id == applicationFeatureId);

        if (link is null)
        {
            return; // Permanently deleted in the meantime.
        }

        ApplyUse(link, now);
        ApplyUse(link.Feature, now);
        ApplyUse(link.Application, now);
        _context.SaveChanges();
    }

    /// <summary>
    /// Writes buffered usage in a single SaveChanges (one transaction): bumps LastUsedAt on links,
    /// applications and features, and adds the counts to the per-day usage rows. Only one flush runs at
    /// a time and nothing else writes usage rows, so the read-modify-write of the counters is safe.
    /// </summary>
    private void WriteUsage(IReadOnlyDictionary<FeatureUsageBuffer.UsageKey, FeatureUsageBuffer.UsageEntry> entries)
    {
        var linkUses = LatestUse(entries, key => key.ApplicationFeatureId);
        var applicationUses = LatestUse(entries, key => key.ApplicationId);
        var featureUses = LatestUse(entries, key => key.FeatureId);

        var linkIds = linkUses.Keys.ToList();
        foreach (var link in _context.ApplicationFeatures.Where(l => linkIds.Contains(l.Id)).ToList())
        {
            ApplyUse(link, linkUses[link.Id]);
        }

        var applicationIds = applicationUses.Keys.ToList();
        foreach (var applicationEntity in _context.Applications.Where(a => applicationIds.Contains(a.Id)).ToList())
        {
            ApplyUse(applicationEntity, applicationUses[applicationEntity.Id]);
        }

        var featureIds = featureUses.Keys.ToList();
        var features = _context.Features.Where(f => featureIds.Contains(f.Id)).ToList();
        foreach (var feature in features)
        {
            ApplyUse(feature, featureUses[feature.Id]);
        }

        // Counts for features permanently deleted since they were used have nothing to attach to.
        var existingFeatureIds = features.Select(f => f.Id).ToHashSet();
        var dailyCounts = entries
            .Where(entry => existingFeatureIds.Contains(entry.Key.FeatureId))
            .GroupBy(entry => (entry.Key.FeatureId, entry.Key.UsageDay))
            .ToDictionary(group => group.Key, group => group.Sum(entry => entry.Value.Count));

        var days = dailyCounts.Keys.Select(key => key.UsageDay).Distinct().ToList();
        var usageRows = _context.FeatureUsage
            .Where(u => featureIds.Contains(u.FeatureId) && days.Contains(u.UsageDay))
            .ToList();

        foreach (var ((featureId, usageDay), count) in dailyCounts)
        {
            var row = usageRows.FirstOrDefault(u => u.FeatureId == featureId && u.UsageDay == usageDay);
            if (row is null)
            {
                _context.FeatureUsage.Add(new FeatureUsageEntity
                {
                    FeatureId = featureId,
                    UsageDay = usageDay,
                    UseCount = count,
                });
            }
            else
            {
                row.UseCount += count;
            }
        }

        _context.SaveChanges();
    }

    private static Dictionary<int, DateTime> LatestUse(
        IReadOnlyDictionary<FeatureUsageBuffer.UsageKey, FeatureUsageBuffer.UsageEntry> entries,
        Func<FeatureUsageBuffer.UsageKey, int> idSelector) =>
        entries
            .GroupBy(entry => idSelector(entry.Key))
            .ToDictionary(group => group.Key, group => group.Max(entry => entry.Value.LastUsedAt));

    /// <summary>
    /// Applies a use that happened at <paramref name="usedAt"/>: bumps LastUsedAt and restores the
    /// entity if it was marked PendingDeletion before that use (the sweep may mark it between a
    /// buffered use and its flush).
    /// </summary>
    private static void ApplyUse(IUsageTrackedEntity entity, DateTime usedAt)
    {
        if (entity.Status == EntityStatus.PendingDeletion
            && (entity.PendingDeletionSince is null || entity.PendingDeletionSince <= usedAt))
        {
            entity.Status = EntityStatus.Active;
            entity.PendingDeletionSince = null;
        }

        if (usedAt > entity.LastUsedAt)
        {
            entity.LastUsedAt = usedAt;
        }
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
