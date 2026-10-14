using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Wookashi.FeatureSwitcher.Client.Abstraction.Exceptions;

namespace Wookashi.FeatureSwitcher.Client.Implementation;

/// <summary>
/// Registers the features on the Node at startup. With <c>allowStartWithoutNode</c>, a failed registration
/// doesn't stop the host; it is retried in the background until it succeeds, so features that are new to
/// the Node stop falling back to their initial states once the Node is reachable again.
/// </summary>
internal sealed class FeatureSwitcherStartupService : IHostedService
{
    // The Node is expected to run close to the application, so retry quickly at first.
    internal static readonly IReadOnlyList<TimeSpan> DefaultRetryDelays =
    [
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(15),
        TimeSpan.FromSeconds(30),
        TimeSpan.FromSeconds(60),
    ];

    // While retrying with the longest delay, a warning is logged only every this many attempts.
    private const int AttemptsPerWarning = 10;

    private readonly FeatureManager _featureManager;
    private readonly bool _allowStartWithoutNode;
    private readonly ILogger<FeatureSwitcherStartupService> _logger;
    private readonly IReadOnlyList<TimeSpan> _retryDelays;
    private readonly CancellationTokenSource _stopping = new();
    private Task? _retryTask;

    public FeatureSwitcherStartupService(
        FeatureManager featureManager,
        bool allowStartWithoutNode = false,
        ILogger<FeatureSwitcherStartupService>? logger = null,
        IReadOnlyList<TimeSpan>? retryDelays = null)
    {
        _featureManager = featureManager;
        _allowStartWithoutNode = allowStartWithoutNode;
        _logger = logger ?? NullLogger<FeatureSwitcherStartupService>.Instance;
        _retryDelays = retryDelays is { Count: > 0 } ? retryDelays : DefaultRetryDelays;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _featureManager.RegisterFeaturesOnNodeAsync(cancellationToken);
        }
        catch (Exception ex) when (_allowStartWithoutNode && IsTransient(ex))
        {
            _logger.LogWarning(
                ex,
                "Feature registration failed at startup. The application starts anyway (allowStartWithoutNode): " +
                "features use their initial states until the Node is reachable, and features new to the Node " +
                "until registration succeeds. Registration is retried in the background.");
            _retryTask = Task.Run(() => RetryRegistrationAsync(_stopping.Token));
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _stopping.Cancel();
        if (_retryTask is not null)
        {
            try
            {
                await _retryTask;
            }
            catch (OperationCanceledException)
            {
                // Expected on shutdown.
            }
        }

        _stopping.Dispose();
    }

    private async Task RetryRegistrationAsync(CancellationToken stoppingToken)
    {
        var elapsed = Stopwatch.StartNew();
        for (var attempt = 1; !stoppingToken.IsCancellationRequested; attempt++)
        {
            await Task.Delay(_retryDelays[Math.Min(attempt - 1, _retryDelays.Count - 1)], stoppingToken);

            try
            {
                await _featureManager.RegisterFeaturesOnNodeAsync(stoppingToken, LogLevel.Debug);
                _logger.LogInformation(
                    "Features registered on the Node after {Attempts} background attempt(s), {Elapsed:hh\\:mm\\:ss} after startup",
                    attempt, elapsed.Elapsed);
                return;
            }
            catch (Exception ex) when (IsTransient(ex) && !stoppingToken.IsCancellationRequested)
            {
                if (attempt < _retryDelays.Count || attempt % AttemptsPerWarning == 0)
                {
                    _logger.LogWarning(
                        "Feature registration still failing (attempt {Attempt}, {Elapsed:hh\\:mm\\:ss} since startup): {Reason}",
                        attempt, elapsed.Elapsed, ex.Message);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // The Node answered with an error that retrying won't fix (e.g. environment mismatch).
                _logger.LogError(ex, "Feature registration failed and will not be retried: {Reason}", ex.Message);
                return;
            }
        }
    }

    // Unreachable Node or a server-side error; a 4xx answer (wrong environment, invalid request) is final.
    private static bool IsTransient(Exception ex) =>
        ex is NodeUnreachableException || ex is RegistrationException { Code: >= 500 };
}
