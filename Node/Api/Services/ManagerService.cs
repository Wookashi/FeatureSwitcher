using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Wookashi.FeatureSwitcher.Node.Api.Configuration;
using Wookashi.FeatureSwitcher.Shared.Abstraction.Models;

namespace Wookashi.FeatureSwitcher.Node.Api.Services;

internal sealed class ManagerService
{
    private readonly ManagerSettings _managerSettings;
    private readonly HttpClient _httpClient;

    internal ManagerService(IOptions<ManagerSettings> managerSettings, IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient();
        _managerSettings = managerSettings.Value;
    }

    internal async Task RegisterNodeToManagerAsync(string nodeEnvironment, Uri nodeAddress)
    {
        var content = new StringContent(JsonSerializer.Serialize(new NodeRegistrationModel{ NodeName = nodeEnvironment,NodeAddress = nodeAddress}), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync($"{_managerSettings.Url}/nodes", content);
        //TODO Implement register method
    }
    
}