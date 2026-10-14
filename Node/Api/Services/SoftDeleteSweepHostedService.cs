using Microsoft.Extensions.Options;
using Wookashi.FeatureSwitcher.Node.Abstraction.Database.Repositories;
using Wookashi.FeatureSwitcher.Node.Api.Configuration;

namespace Wookashi.FeatureSwitcher.Node.Api.Services;

/// <summary>
/// Periodically marks application-feature links (and then applications) as PendingDeletion when they
/// have not been registered or read for longer than the configured stale threshold. Links that are
/// read again before permanent deletion are auto-restored at read time by <see cref="FeatureService"/>.
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
        var usageRetention = _config.UsageRetention;
        if (usageRetention > TimeSpan.Zero && usageRetention < NodeConfiguration.MinUsageRetention)
        {
            _logger.LogWarning(
                "UsageRetention {Configured} is shorter than the 7-day read count shown in the Manager; using {Minimum}",
                usageRetention, NodeConfiguration.MinUsageRetention);
            usageRetention = NodeConfiguration.MinUsageRetention;
        }

        _logger.LogInformation(
            "SoftDeleteSweepHostedService started. StaleAfter={StaleAfter}, Interval={Interval}, UsageRetention={UsageRetention}",
            staleAfter, interval, usageRetention > TimeSpan.Zero ? (object)usageRetention : "disabled");

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

            if (usageRetention > TimeSpan.Zero)
            {
                try
                {
                    DeleteOldUsage(usageRetention);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Usage retention cleanup failed");
                }
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

    private void DeleteOldUsage(TimeSpan retention)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IFeatureRepository>();

        var threshold = DateTime.UtcNow - retention;
        var deleted = repository.DeleteUsageOlderThan(threshold);

        if (deleted > 0)
        {
            _logger.LogInformation(
                "Usage retention deleted {Count} daily read count(s) before {Threshold:yyyy-MM-dd}", deleted, threshold);
        }
        else
        {
            _logger.LogDebug("Usage retention found no daily read counts before {Threshold:yyyy-MM-dd}", threshold);
        }
    }

    private void Sweep(TimeSpan staleAfter)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IFeatureRepository>();

        // Write buffered usage first, so a feature used since the last flush is not judged stale on
        // outdated LastUsedAt values. If that fails, the sweep is skipped rather than run on them.
        repository.FlushFeatureUsage();

        var threshold = DateTime.UtcNow - staleAfter;

        var applicationFeaturesMarked = repository.MarkStaleApplicationFeaturesPending(threshold);
        var appsMarked = repository.MarkStaleApplicationsPending(threshold);

        if (applicationFeaturesMarked > 0 || appsMarked > 0)
        {
            _logger.LogInformation(
                "Soft-delete sweep marked {ApplicationFeaturesMarked} application-feature link(s) and {AppsMarked} application(s) as PendingDeletion (threshold {Threshold:o})",
                applicationFeaturesMarked, appsMarked, threshold);
        }
        else
        {
            _logger.LogDebug("Soft-delete sweep found nothing to mark (threshold {Threshold:o})", threshold);
        }
    }
}
