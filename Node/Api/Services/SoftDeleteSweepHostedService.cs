using Microsoft.Extensions.Options;
using Wookashi.FeatureSwitcher.Node.Abstraction.Database.Repositories;
using Wookashi.FeatureSwitcher.Node.Api.Configuration;

namespace Wookashi.FeatureSwitcher.Node.Api.Services;

/// <summary>
/// Periodically marks features (and then applications) as PendingDeletion when they have not been
/// registered or read for longer than the configured stale threshold. Features that are read again
/// before permanent deletion are auto-restored at read time by <see cref="FeatureService"/>.
/// </summary>
internal sealed class SoftDeleteSweepHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SoftDeleteSweepHostedService> _logger;
    private readonly NodeConfiguration _config;

    public SoftDeleteSweepHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<NodeConfiguration> options,
        ILogger<SoftDeleteSweepHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _config = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var staleAfter = _config.FeatureStaleAfter;
        var interval = _config.FeatureCleanupInterval;

        _logger.LogInformation(
            "SoftDeleteSweepHostedService started. StaleAfter={StaleAfter}, Interval={Interval}",
            staleAfter, interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                Sweep(staleAfter);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Soft-delete sweep failed");
            }

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private void Sweep(TimeSpan staleAfter)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IFeatureRepository>();

        var threshold = DateTime.UtcNow - staleAfter;

        var featuresMarked = repository.MarkStaleFeaturesPending(threshold);
        var appsMarked = repository.MarkStaleApplicationsPending(threshold);

        if (featuresMarked > 0 || appsMarked > 0)
        {
            _logger.LogInformation(
                "Soft-delete sweep marked {FeaturesMarked} feature(s) and {AppsMarked} application(s) as PendingDeletion (threshold {Threshold:o})",
                featuresMarked, appsMarked, threshold);
        }
        else
        {
            _logger.LogDebug("Soft-delete sweep found nothing to mark (threshold {Threshold:o})", threshold);
        }
    }
}
