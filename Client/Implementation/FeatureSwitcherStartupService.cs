using Microsoft.Extensions.Hosting;

namespace Wookashi.FeatureSwitcher.Client.Implementation;

internal sealed class FeatureSwitcherStartupService : IHostedService
{
    private readonly FeatureManager _featureManager;

    public FeatureSwitcherStartupService(FeatureManager featureManager)
    {
        _featureManager = featureManager;
    }

    public Task StartAsync(CancellationToken cancellationToken)
        => _featureManager.RegisterFeaturesOnNodeAsync(cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;
}
