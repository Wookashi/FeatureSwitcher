using Microsoft.Extensions.DependencyInjection;
using Wookashi.FeatureSwitcher.Node.Abstraction.Database.Dtos;
using Wookashi.FeatureSwitcher.Node.Abstraction.Database.Repositories;
using Wookashi.FeatureSwitcher.Node.Abstraction.Infrastructure.Exceptions;
using Wookashi.FeatureSwitcher.Node.Database.Extensions;

namespace Wookashi.FeatureSwitcher.Node.Database.Tests;

public sealed class FeatureRepositoryLifecycleTests
{
    [Fact]
    public void PermanentlyDeleteFeature_RemovesOnlyStaleApplicationFeatureLink_WhenFeatureIsShared()
    {
        var services = new ServiceCollection();
        services.AddDatabase(string.Empty);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var repository = scope.ServiceProvider.GetRequiredService<IFeatureRepository>();
        var context = scope.ServiceProvider.GetRequiredService<IFeaturesDataContext>();

        var id = Guid.NewGuid().ToString("N");
        var appA = new ApplicationDto($"DeleteAppA-{id}");
        var appB = new ApplicationDto($"DeleteAppB-{id}");
        var sharedFeature = new FeatureDto($"DeleteSharedCheckout-{id}", true);

        repository.RegisterApplication(appA, [sharedFeature]);
        repository.RegisterApplication(appB, [sharedFeature]);

        var staleLastUsedAt = DateTime.UtcNow.AddDays(-40);
        var appALink = context.ApplicationFeatures
            .Single(link => link.Application.Name == appA.Name && link.Feature.Name == sharedFeature.Name);
        appALink.LastUsedAt = staleLastUsedAt;
        context.SaveChanges();

        var marked = repository.MarkStaleApplicationFeaturesPending(DateTime.UtcNow.AddDays(-30));

        Assert.Equal(1, marked);

        var pendingFeature = Assert.Single(repository.GetPendingFeatures());
        Assert.Equal(appA.Name, pendingFeature.ApplicationName);
        Assert.Equal(sharedFeature.Name, pendingFeature.FeatureName);
        Assert.Equal(staleLastUsedAt, pendingFeature.LastUsedAt);

        var deletionResult = repository.PermanentlyDeleteFeature(appA.Name, sharedFeature.Name);

        Assert.Equal(1, deletionResult.RemovedApplicationFeatureLinks);
        Assert.Equal(0, deletionResult.DeletedFeatures);

        Assert.Throws<FeatureNotFoundException>(() =>
            repository.GetFeatureState(appA, sharedFeature.Name));
        Assert.True(repository.GetFeatureState(appB, sharedFeature.Name));
        Assert.Single(context.Features.Where(feature => feature.Name == sharedFeature.Name));
        Assert.Single(context.ApplicationFeatures.Where(link => link.Feature.Name == sharedFeature.Name));
    }

    [Fact]
    public void UpdateFeature_ReturnsSharedScope_WhenFeatureIsUsedByMultipleApplications()
    {
        var services = new ServiceCollection();
        services.AddDatabase(string.Empty);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var repository = scope.ServiceProvider.GetRequiredService<IFeatureRepository>();

        var id = Guid.NewGuid().ToString("N");
        var appA = new ApplicationDto($"UpdateAppA-{id}");
        var appB = new ApplicationDto($"UpdateAppB-{id}");
        var sharedFeature = new FeatureDto($"UpdateSharedCheckout-{id}", true);

        repository.RegisterApplication(appA, [sharedFeature]);
        repository.RegisterApplication(appB, [sharedFeature]);

        var result = repository.UpdateFeature(appA, new FeatureDto(sharedFeature.Name, false));

        Assert.Equal(sharedFeature.Name, result.FeatureName);
        Assert.False(result.State);
        Assert.Equal(2, result.AffectedApplications);
        Assert.True(result.IsShared);
        Assert.False(repository.GetFeatureState(appA, sharedFeature.Name));
        Assert.False(repository.GetFeatureState(appB, sharedFeature.Name));
    }
}
