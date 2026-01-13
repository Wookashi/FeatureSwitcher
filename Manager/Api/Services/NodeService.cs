using System.Net;
using System.Text;
using System.Text.Json;
using Wookashi.FeatureSwitcher.Manager.Abstraction.Database.Dtos;
using Wookashi.FeatureSwitcher.Manager.Abstraction.Database.Repositories;
using Wookashi.FeatureSwitcher.Shared.Abstraction.Models;

namespace Wookashi.FeatureSwitcher.Manager.Api.Services;

internal sealed class NodeService
{
    private readonly INodeRepository _nodeRepository;
    private readonly HttpClient _httpClient;

    public NodeService(INodeRepository nodeRepository, IHttpClientFactory httpClientFactory)
    {
        _nodeRepository = nodeRepository;
        _httpClient = httpClientFactory.CreateClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(5);
    }

    public void CreateOrReplaceNode(NodeRegistrationModel nodeRegistrationModel)
    {
        _nodeRepository
            .CreateOrUpdateNode(nodeRegistrationModel.NodeName, nodeRegistrationModel.NodeAddress);
    }

    public List<NodeDto> GetAllNodes()
    {
        return _nodeRepository.GetAllNodes();
    }

    public async Task<List<ApplicationDto>> GetApplicationsAsync(int nodeId, CancellationToken ct = default)
    {
        var node = _nodeRepository.GetNodeById(nodeId);
        if (node is null)
            throw new KeyNotFoundException($"Node with id={nodeId} not found.");

        var url = JoinUrl(node.Address, "/applications");

        var apps = await _httpClient.GetFromJsonAsync<List<ApplicationDto>>(url, ct);
        return apps ?? [];
    }

    public async Task<List<FeatureDto>> GetFeaturesForApplicationAsync(int nodeId, string appName, CancellationToken ct = default)
    {
        var node = _nodeRepository.GetNodeById(nodeId);
        if (node is null)
            throw new KeyNotFoundException($"Node with id={nodeId} not found.");

        var appNameEncoded = Uri.EscapeDataString(appName);

        var url = JoinUrl(node.Address, $"/applications/{appNameEncoded}/features");

        var features = await _httpClient.GetFromJsonAsync<List<FeatureDto>>(url, ct);
        return features ?? [];
    }

    public async Task<HttpResponseMessage> SetFeatureStateAsync(int nodeId, string appName, string featureName, FeatureStateModel featureState, CancellationToken ct = default)
    {
        var node = _nodeRepository.GetNodeById(nodeId);
        if (node is null)
            throw new KeyNotFoundException($"Node with id={nodeId} not found.");

        var appNameEncoded = Uri.EscapeDataString(appName);
        var featureNameEncoded = Uri.EscapeDataString(featureName);

        var url = JoinUrl(node.Address, $"/applications/{appNameEncoded}/features/{featureNameEncoded}");

        var content = new StringContent(JsonSerializer.Serialize(featureState), Encoding.UTF8, "application/json");
        
        return await _httpClient.PutAsync(url, content, ct);
    }

    private static string JoinUrl(string baseAddress, string path)
    {
        if (string.IsNullOrWhiteSpace(baseAddress))
            throw new ArgumentException("Base address is empty.", nameof(baseAddress));

        if (!baseAddress.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !baseAddress.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"Invalid node address (missing scheme): {baseAddress}", nameof(baseAddress));
        }

        var baseUri = baseAddress.EndsWith('/') ? new Uri(baseAddress) : new Uri($"{baseAddress}/");
        var rel = path.StartsWith('/') ? path.Substring(1) : path;
        return new Uri(baseUri, rel).ToString();
    }
}