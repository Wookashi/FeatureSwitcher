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
    private readonly ILogger<NodeService> _logger;

    public NodeService(INodeRepository nodeRepository, IHttpClientFactory httpClientFactory, ILogger<NodeService> logger)
    {
        _nodeRepository = nodeRepository;
        _httpClient = httpClientFactory.CreateClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(5);
        _logger = logger;
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
        _logger.LogInformation("Fetching applications from node {NodeId} at {Url}", nodeId, url);

        try
        {
            var apps = await _httpClient.GetFromJsonAsync<List<ApplicationDto>>(url, ct);
            _logger.LogInformation("Retrieved {Count} application(s) from node {NodeId}", apps?.Count ?? 0, nodeId);
            return apps ?? [];
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Node {NodeId} ({Url}) returned an error while fetching applications. StatusCode: {StatusCode}", nodeId, url, ex.StatusCode);
            throw;
        }
        catch (OperationCanceledException ex)
        {
            if (!ct.IsCancellationRequested)
                _logger.LogError(ex, "Request to node {NodeId} ({Url}) timed out while fetching applications", nodeId, url);
            throw;
        }
    }

    public async Task<List<FeatureDto>> GetFeaturesForApplicationAsync(int nodeId, string appName, CancellationToken ct = default)
    {
        var node = _nodeRepository.GetNodeById(nodeId);
        if (node is null)
            throw new KeyNotFoundException($"Node with id={nodeId} not found.");

        var appNameEncoded = Uri.EscapeDataString(appName);
        var url = JoinUrl(node.Address, $"/applications/{appNameEncoded}/features");
        _logger.LogInformation("Fetching features for app {AppName} from node {NodeId} at {Url}", appName, nodeId, url);

        try
        {
            var features = await _httpClient.GetFromJsonAsync<List<FeatureDto>>(url, ct);
            _logger.LogInformation("Retrieved {Count} feature(s) for app {AppName} from node {NodeId}", features?.Count ?? 0, appName, nodeId);
            return features ?? [];
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Node {NodeId} ({Url}) returned an error while fetching features for app {AppName}. StatusCode: {StatusCode}", nodeId, url, appName, ex.StatusCode);
            throw;
        }
        catch (OperationCanceledException ex)
        {
            if (!ct.IsCancellationRequested)
                _logger.LogError(ex, "Request to node {NodeId} ({Url}) timed out while fetching features for app {AppName}", nodeId, url, appName);
            throw;
        }
    }

    public async Task<HttpResponseMessage> SetFeatureStateAsync(int nodeId, string appName, string featureName, FeatureStateModel featureState, CancellationToken ct = default)
    {
        var node = _nodeRepository.GetNodeById(nodeId);
        if (node is null)
            throw new KeyNotFoundException($"Node with id={nodeId} not found.");

        var appNameEncoded = Uri.EscapeDataString(appName);
        var featureNameEncoded = Uri.EscapeDataString(featureName);
        var url = JoinUrl(node.Address, $"/applications/{appNameEncoded}/features/{featureNameEncoded}");
        _logger.LogInformation("Setting feature {FeatureName} for app {AppName} on node {NodeId} to {State}", featureName, appName, nodeId, featureState.State);

        try
        {
            var content = new StringContent(JsonSerializer.Serialize(featureState), Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync(url, content, ct);
            _logger.LogInformation("Set feature {FeatureName} on node {NodeId}: response {StatusCode}", featureName, nodeId, (int)response.StatusCode);
            return response;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Node {NodeId} ({Url}) unreachable while setting feature {FeatureName} state. StatusCode: {StatusCode}", nodeId, url, featureName, ex.StatusCode);
            return new HttpResponseMessage(HttpStatusCode.BadGateway);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogError(ex, "Request to node {NodeId} ({Url}) timed out while setting feature {FeatureName} state", nodeId, url, featureName);
            return new HttpResponseMessage(HttpStatusCode.GatewayTimeout);
        }
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