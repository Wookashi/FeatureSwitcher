using System.Net.Sockets;
using System.Text.Json;
using Wookashi.FeatureSwitcher.Client.Abstraction;
using Wookashi.FeatureSwitcher.Client.Abstraction.Exceptions;
using Wookashi.FeatureSwitcher.Client.Implementation.Models;

namespace Wookashi.FeatureSwitcher.Client.Implementation;

public class FeatureManager : IFeatureManager
{
    private readonly List<FeatureStateModel> _features;
    private readonly string _appName;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly Uri _nodeAddress = new("http://localhost:5216");

    public FeatureManager(IHttpClientFactory httpClientFactory, string appName, List<FeatureStateModel> features,
        Uri? nodeAddress = null)
    {
        _appName = appName;
        _features = features;
        _httpClientFactory = httpClientFactory;
        if (nodeAddress is not null)
        {
            _nodeAddress = nodeAddress;
        }
    }

    public async Task<bool> IsFeatureEnabledAsync(string featureName)
    {
        var collectionFeature = _features.FirstOrDefault(feature => feature.Name == featureName);
        if (collectionFeature is null)
        {
            throw new FeatureNotRegisteredException("Feature is not registered yet!");
        }

        bool? nodeState = null;
        try
        {
            nodeState = await IsFeatureEnabledOnNode(featureName);
        }
        catch (NodeUnreachableException)
        {
            //do nothing
        }

        if (nodeState is not null)
        {
            collectionFeature.CurrentLocalState = nodeState.Value;
        }

        return collectionFeature.CurrentLocalState;
    }

    private async Task<bool> IsFeatureEnabledOnNode(string featureName)
    {
        bool model;
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.GetAsync($"{_nodeAddress}/applications/{_appName}/features/{featureName}/state/");

            if (response.IsSuccessStatusCode)
            {
                var res = response.Content.ReadAsStringAsync().Result;
                model = JsonSerializer.Deserialize<bool>(res);
            }
            else
            {
                throw new NodeUnreachableException(response.ReasonPhrase ?? "Unknown error");
            }
        }
        catch (HttpRequestException requestException) when (requestException.InnerException is SocketException)
        {
            throw new NodeUnreachableException(requestException.Message,
                ((SocketException)requestException.InnerException).ErrorCode);
        }
        catch (Exception exc)
        {
            throw new NodeUnreachableException(exc.Message);
        }

        return model;
    }
}