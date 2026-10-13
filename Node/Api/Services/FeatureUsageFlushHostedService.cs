using Microsoft.Extensions.Options;
using Wookashi.FeatureSwitcher.Node.Abstraction.Database.Repositories;
using Wookashi.FeatureSwitcher.Node.Api.Configuration;

namespace Wookashi.FeatureSwitcher.Node.Api.Services;

/// <summary>
/// Periodically writes feature usage buffered by flag-state reads and registrations
/// (see <see cref="IFeatureRepository.FlushFeatureUsage"/>), and once more on graceful shutdown.
/// </summary>
internal sealed class FeatureUsageFlushHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<NodeConfiguration> options,
    ILogger<FeatureUsageFlushHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = options.Value.UsageFlushInterval;
        if (interval <= TimeSpan.Zero)
        {
            logger.LogWarning("UsageFlushInterval {Interval} is not positive; using the default {Default}",
                interval, NodeConfiguration.DefaultUsageFlushInterval);
            interval = NodeConfiguration.DefaultUsageFlushInterval;
        }

        using var timer = new PeriodicTimer(interval);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                Flush();
            }
        }
        catch (OperationCanceledException)
        {
            // Shutting down — StopAsync does the final flush.
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        Flush();
    }

    private void Flush()
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IFeatureRepository>();
            var written = repository.FlushFeatureUsage();
            if (written > 0)
            {
                logger.LogDebug("Wrote {Count} buffered feature usage entr(ies)", written);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to write buffered feature usage; it stays buffered and is retried on the next flush");
        }
    }
}
