using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Wookashi.FeatureSwitcher.Node.Api.Configuration;
using Wookashi.FeatureSwitcher.Shared.Abstraction.Models;

namespace Wookashi.FeatureSwitcher.Node.Api.Services;

internal sealed class ManagerRegistrationHostedService(
    IOptions<ManagerSettings> managerSettings,
    IOptions<NodeConfiguration> nodeConfiguration,
    IHttpClientFactory httpClientFactory,
    ILogger<ManagerRegistrationHostedService> logger) : BackgroundService
{
    /// <summary>
    /// Stepped retry schedule. The Manager may be unavailable for a long time (planned downtime, outages),
    /// so the node must be able to wait it out without hammering it: a quick first retry covers
    /// containers starting together, then a few short retries cover a Manager restart, and after that
    /// the node settles into a steady 5-minute cadence for as long as the outage lasts.
    /// </summary>
    private static readonly TimeSpan[] RetrySchedule =
    [
        TimeSpan.FromSeconds(10),
        TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1),
        TimeSpan.FromMinutes(3), TimeSpan.FromMinutes(3), TimeSpan.FromMinutes(3),
        TimeSpan.FromMinutes(5),
    ];

    /// <summary>
    /// Random spread applied to each delay so several nodes don't all hit the Manager at the same
    /// moment when it comes back.
    /// </summary>
    private const double RetryJitter = 0.15;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = managerSettings.Value;
        var nodeConfig = nodeConfiguration.Value;

        if (string.IsNullOrEmpty(settings.Url) ||
            string.IsNullOrEmpty(nodeConfig.Name) ||
            string.IsNullOrEmpty(nodeConfig.Address))
        {
            logger.LogWarning("Manager registration skipped: ManagerSettings.Url, NodeConfiguration.Name, or NodeConfiguration.Address not fully configured");
            return;
        }

        var httpClient = httpClientFactory.CreateClient("Manager");
        var attempt = 0;
        var outage = Stopwatch.StartNew();

        while (!stoppingToken.IsCancellationRequested)
        {
            attempt++;
            try
            {
                // Authenticate with manager if credentials are configured
                if (!string.IsNullOrEmpty(settings.Username) && !string.IsNullOrEmpty(settings.Password))
                {
                    var loginPayload = JsonSerializer.Serialize(new { username = settings.Username, password = settings.Password });
                    using var loginContent = new StringContent(loginPayload, Encoding.UTF8, "application/json");

                    using var loginResponse = await httpClient.PostAsync($"{settings.Url}/api/auth/login", loginContent, stoppingToken);

                    // Only a definitive credentials rejection stops retrying. Anything else (e.g. 502/503
                    // from a reverse proxy while the Manager is still starting) is transient.
                    if (loginResponse.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                    {
                        logger.LogError(
                            "Authentication with manager failed (HTTP {StatusCode}). " +
                            "Make sure the initial admin setup has been completed at the Manager UI " +
                            "and that the configured credentials (ManagerSettings__Username / ManagerSettings__Password) " +
                            "match an existing Admin account. " +
                            "Restart this node after completing setup.",
                            (int)loginResponse.StatusCode);
                        return;
                    }

                    if (!loginResponse.IsSuccessStatusCode)
                    {
                        throw new HttpRequestException(
                            $"Manager returned HTTP {(int)loginResponse.StatusCode} for login",
                            inner: null,
                            loginResponse.StatusCode);
                    }

                    var loginJson = await loginResponse.Content.ReadAsStringAsync(stoppingToken);
                    using var loginDocument = JsonDocument.Parse(loginJson);
                    var token = loginDocument.RootElement.GetProperty("token").GetString();

                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }

                var registrationModel = new NodeRegistrationModel
                {
                    NodeName = nodeConfig.Name,
                    NodeAddress = new Uri(nodeConfig.Address)
                };

                using var content = new StringContent(
                    JsonSerializer.Serialize(registrationModel),
                    Encoding.UTF8,
                    "application/json");

                using var registrationResponse = await httpClient.PutAsync($"{settings.Url}/api/nodes", content, stoppingToken);

                if (!registrationResponse.IsSuccessStatusCode)
                {
                    throw new HttpRequestException(
                        $"Manager returned HTTP {(int)registrationResponse.StatusCode} for node registration",
                        inner: null,
                        registrationResponse.StatusCode);
                }

                if (attempt == 1)
                {
                    logger.LogInformation("Node '{NodeName}' registered with manager at {ManagerUrl}", nodeConfig.Name, settings.Url);
                }
                else
                {
                    logger.LogInformation(
                        "Node '{NodeName}' registered with manager at {ManagerUrl} after {Attempts} attempts ({Elapsed:g} since first attempt)",
                        nodeConfig.Name, settings.Url, attempt, outage.Elapsed);
                }
                return;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                var delay = GetRetryDelay(attempt, Random.Shared.NextDouble());
                logger.LogWarning(ex,
                    "Failed to register node with manager at {ManagerUrl} (attempt {Attempt}, {Elapsed:g} since first attempt), next attempt in {WaitSeconds} seconds",
                    settings.Url, attempt, outage.Elapsed, (int)delay.TotalSeconds);

                try
                {
                    await Task.Delay(delay, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }

    /// <param name="attempt">1-based number of the attempt that just failed.</param>
    /// <param name="random">Value in [0, 1) used to spread the delay by ±<see cref="RetryJitter"/>.</param>
    internal static TimeSpan GetRetryDelay(int attempt, double random)
    {
        var baseDelay = RetrySchedule[Math.Clamp(attempt - 1, 0, RetrySchedule.Length - 1)];
        var factor = 1 + (random * 2 - 1) * RetryJitter;
        return baseDelay * factor;
    }
}
