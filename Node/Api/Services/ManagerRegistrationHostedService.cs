using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Wookashi.FeatureSwitcher.Node.Api.Configuration;
using Wookashi.FeatureSwitcher.Shared.Abstraction.Models;

namespace Wookashi.FeatureSwitcher.Node.Api.Services;

internal sealed class ManagerRegistrationHostedService(
    IOptions<ManagerSettings> managerSettings,
    IHttpClientFactory httpClientFactory,
    ILogger<ManagerRegistrationHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var settings = managerSettings.Value;

        if (string.IsNullOrEmpty(settings.Url) ||
            string.IsNullOrEmpty(settings.NodeName) ||
            string.IsNullOrEmpty(settings.NodeAddress))
        {
            logger.LogWarning("Manager registration skipped: ManagerSettings (Url, NodeName, NodeAddress) not fully configured");
            return;
        }

        try
        {
            var httpClient = httpClientFactory.CreateClient();

            // Authenticate with manager if credentials are configured
            if (!string.IsNullOrEmpty(settings.Username) && !string.IsNullOrEmpty(settings.Password))
            {
                var loginPayload = JsonSerializer.Serialize(new { username = settings.Username, password = settings.Password });
                var loginContent = new StringContent(loginPayload, Encoding.UTF8, "application/json");

                var loginResponse = await httpClient.PostAsync($"{settings.Url}/api/auth/login", loginContent, cancellationToken);
                loginResponse.EnsureSuccessStatusCode();

                var loginJson = await loginResponse.Content.ReadAsStringAsync(cancellationToken);
                var token = JsonDocument.Parse(loginJson).RootElement.GetProperty("token").GetString();

                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            var registrationModel = new NodeRegistrationModel
            {
                NodeName = settings.NodeName,
                NodeAddress = new Uri(settings.NodeAddress)
            };

            var content = new StringContent(
                JsonSerializer.Serialize(registrationModel),
                Encoding.UTF8,
                "application/json");

            await httpClient.PutAsync($"{settings.Url}/api/nodes", content, cancellationToken);

            logger.LogInformation("Node '{NodeName}' registered with manager at {ManagerUrl}", settings.NodeName, settings.Url);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to register node with manager at {ManagerUrl}", settings.Url);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
