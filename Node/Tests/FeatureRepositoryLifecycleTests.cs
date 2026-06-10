using Microsoft.Extensions.DependencyInjection;
using Wookashi.FeatureSwitcher.Node.Abstraction.Database.Dtos;
using Wookashi.FeatureSwitcher.Node.Abstraction.Database.Repositories;
using Wookashi.FeatureSwitcher.Node.Abstraction.Infrastructure.Exceptions;
using Wookashi.FeatureSwitcher.Node.Database;
using Wookashi.FeatureSwitcher.Node.Database.Entities;
using Wookashi.FeatureSwitcher.Node.Database.Extensions;

namespace Node.Database.Tests;

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

        var appA = new ApplicationDto("AppA");
        var appB = new ApplicationDto("AppB");
        var sharedFeature = new FeatureDto("SharedCheckout", true);

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

        repository.PermanentlyDeleteFeature(appA.Name, sharedFeature.Name);

        Assert.Throws<FeatureNotFoundException>(() =>
            repository.GetFeatureState(appA, sharedFeature.Name));
        Assert.True(repository.GetFeatureState(appB, sharedFeature.Name));
        Assert.Single(context.Features);
        Assert.Single(context.ApplicationFeatures);
    }
}
