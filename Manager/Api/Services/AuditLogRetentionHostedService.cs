using Microsoft.Extensions.Options;
using Wookashi.FeatureSwitcher.Manager.Abstraction.Database.Repositories;
using Wookashi.FeatureSwitcher.Manager.Api.Configuration;

namespace Wookashi.FeatureSwitcher.Manager.Api.Services;

/// <summary>
/// Deletes audit log entries older than <see cref="AuditLogSettings.Retention"/>: once at startup and
/// then once a day. Entries are deleted in batches, each in its own short transaction, so a large
/// backlog (e.g. the first run after an upgrade) doesn't hold the SQLite write lock for long.
/// </summary>
internal sealed class AuditLogRetentionHostedService : BackgroundService
{
    internal const int BatchSize = 1000;
    private static readonly TimeSpan Interval = TimeSpan.FromDays(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AuditLogRetentionHostedService> _logger;
    private readonly AuditLogSettings _settings;

    public AuditLogRetentionHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<AuditLogSettings> options,
        ILogger<AuditLogRetentionHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _settings = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var retention = _settings.Retention;
        if (retention <= TimeSpan.Zero)
        {
            _logger.LogInformation("Audit log retention disabled; entries are kept indefinitely");
            return;
        }

        _logger.LogInformation("Audit log retention: entries older than {Retention} are deleted daily", retention);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                Purge(retention, stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Audit log retention cleanup failed");
            }

            try
            {
                await Task.Delay(Interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private void Purge(TimeSpan retention, CancellationToken stoppingToken)
    {
        var threshold = DateTime.UtcNow - retention;
        var total = 0;

        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IAuditLogRepository>();

        int deleted;
        do
        {
            deleted = repository.DeleteEntriesOlderThan(threshold, BatchSize);
            total += deleted;
        } while (deleted == BatchSize && !stoppingToken.IsCancellationRequested);

        if (total > 0)
        {
            _logger.LogInformation(
                "Audit log retention deleted {Count} entr(ies) older than {Threshold:o}", total, threshold);
        }
        else
        {
            _logger.LogDebug("Audit log retention found no entries older than {Threshold:o}", threshold);
        }
    }
}
