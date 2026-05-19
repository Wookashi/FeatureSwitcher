using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Wookashi.FeatureSwitcher.Client.Abstraction.Exceptions;

namespace Wookashi.FeatureSwitcher.Client.Implementation;

internal sealed class FeatureSwitcherStartupService : IHostedService
{
    private readonly FeatureManager _featureManager;
    private readonly bool _allowStartWithoutNode;
    private readonly ILogger<FeatureSwitcherStartupService> _logger;

    public FeatureSwitcherStartupService(
        FeatureManager featureManager,
        bool allowStartWithoutNode = false,
        ILogger<FeatureSwitcherStartupService>? logger = null)
    {
        _featureManager = featureManager;
        _allowStartWithoutNode = allowStartWithoutNode;
        _logger = logger ?? NullLogger<FeatureSwitcherStartupService>.Instance;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _featureManager.RegisterFeaturesOnNodeAsync(cancellationToken);
        }
        catch (NodeUnreachableException ex) when (_allowStartWithoutNode)
        {
            _logger.LogWarning(
                ex,
                "Node unreachable during startup registration. Application will start with cached/initial feature states.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;
}
