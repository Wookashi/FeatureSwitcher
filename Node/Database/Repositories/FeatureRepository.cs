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

    public List<ApplicationDto> GetApplications()
    {
        return _context.Applications
            .Select(application => new ApplicationDto(application.Name))
            .ToList();
    }

    public List<FeatureDto> GetFeaturesForApplication(ApplicationDto application)
    {
        var list = _context.Features
            .Where(feature => feature.Application.Name == application.Name)
            .Select(feature => new FeatureDto(
                feature.Name,
                feature.IsEnabled))
            .ToList();
        return list;
    }

    public bool GetFeatureState(ApplicationDto application, string featureName)
    {
        var featureEntity = _context.Features
            .FirstOrDefault(feature => feature.Application.Name == application.Name
                                       && feature.Name == featureName);

        return featureEntity?.IsEnabled ?? throw new FeatureNotFoundException("Feature not found");
    }

    public void UpdateFeature(ApplicationDto application, FeatureDto featureDto)
    {
        var featureEntity = _context.Features
            .FirstOrDefault(feature => feature.Application.Name == application.Name
                                       && feature.Name == featureDto.Name);

        if (featureEntity is null)
        {
            throw new FeatureNotFoundException("Feature not found");
        }

        featureEntity.IsEnabled = featureDto.State;
        _context.SaveChanges();
    }

    public void AddFeaturesForApplication(ApplicationDto application, List<FeatureDto> featuresList)
    {
        if (featuresList.Count == 0)
        {
            return;
        }

        var applicationEntity = _context.Applications
            .FirstOrDefault(app => app.Name == application.Name);
        if (applicationEntity is null)
        {
            _context.Applications.Add(new ApplicationEntity
            {
                Name = application.Name,
            });
            _context.SaveChanges();
            applicationEntity = _context.Applications
                .FirstOrDefault(app => app.Name == application.Name);
        }

        var featureEntities = featuresList.Select(feature => new FeatureEntity
        {
            Name = feature.Name,
            IsEnabled = feature.State,
            Application = applicationEntity ?? throw new InvalidOperationException(),
        });
        _context.Features.AddRange(featureEntities);
        _context.SaveChanges();
    }

    public void DeleteFeaturesForApplication(ApplicationDto application, List<FeatureDto> featuresList)
    {
        if (featuresList.Count == 0)
        {
            return;
        }


        var applicationEntity = _context.Applications
            .Include(applicationEntity => applicationEntity.Features)
            .FirstOrDefault(app => app.Name == application.Name);

        if (applicationEntity is null)
        {
            return;
        }

        foreach (var featureEntity in applicationEntity.Features)
        {
            _context.Features.Remove(featureEntity);
        }

        _context.SaveChanges();
    }
}