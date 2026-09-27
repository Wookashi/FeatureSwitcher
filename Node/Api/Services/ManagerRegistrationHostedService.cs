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
    private static readonly TimeSpan MaxRetryDelay = TimeSpan.FromMinutes(5);

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

        while (!stoppingToken.IsCancellationRequested)
        {
            attempt++;
            try
            {
                // Authenticate with manager if credentials are configured
                if (!string.IsNullOrEmpty(settings.Username) && !string.IsNullOrEmpty(settings.Password))
                {
                    var loginPayload = JsonSerializer.Serialize(new { username = settings.Username, password = settings.Password });
                    var loginContent = new StringContent(loginPayload, Encoding.UTF8, "application/json");

                    var loginResponse = await httpClient.PostAsync($"{settings.Url}/api/auth/login", loginContent, stoppingToken);

                    if (!loginResponse.IsSuccessStatusCode)
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

                    var loginJson = await loginResponse.Content.ReadAsStringAsync(stoppingToken);
                    var token = JsonDocument.Parse(loginJson).RootElement.GetProperty("token").GetString();

                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }

                var registrationModel = new NodeRegistrationModel
                {
                    NodeName = nodeConfig.Name,
                    NodeAddress = new Uri(nodeConfig.Address)
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(registrationModel),
                    Encoding.UTF8,
                    "application/json");

                var registrationResponse = await httpClient.PutAsync($"{settings.Url}/api/nodes", content, stoppingToken);

                if (!registrationResponse.IsSuccessStatusCode)
                {
                    throw new HttpRequestException(
                        $"Manager returned HTTP {(int)registrationResponse.StatusCode} for node registration");
                }

                logger.LogInformation("Node '{NodeName}' registered with manager at {ManagerUrl}", nodeConfig.Name, settings.Url);
                return;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                var waitSeconds = Math.Min(attempt + 50, (int)MaxRetryDelay.TotalSeconds);
                logger.LogWarning(ex, "Failed to register node with manager at {ManagerUrl}, next attempt after {WaitSeconds} seconds", settings.Url, waitSeconds);

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(waitSeconds), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }
}
