using System.Text.Json;
using Wookashi.FeatureSwitcher.Client.Abstraction;
using Wookashi.FeatureSwitcher.Client.Abstraction.Exceptions;
using Wookashi.FeatureSwitcher.Client.Implementation.Models;

namespace Wookashi.FeatureSwitcher.Client.Implementation;

public class FeatureManager : IFeatureManager
{
    private List<FeatureStateModel> _features;
    private readonly string _appName;
    private IHttpClientFactory _httpClientFactory;
    private Uri _nodeAddress = new("http://localhost:5216");

    public FeatureManager(string appName, List<FeatureStateModel> features, Uri? nodeAddress = null)
    {
        _appName = appName;
        _features = features;
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
            throw new FeatureNotRegisteredException("Feature is not registered in node!");
        }

        var nodeState = await IsFeatureEnabledOnNode(featureName);

        return collectionFeature.CurrentLocalState;
    }

    private async Task<bool> IsFeatureEnabledOnNode(string featureName)
    {
        var httpClient = _httpClientFactory.CreateClient();
        var test = new FeatureStateModel("test", true);
        var response = await httpClient.PostAsync("/feature-state", new StringContent(JsonSerializer.Serialize(test)));

        var model = false;
        if (response.IsSuccessStatusCode)
        {
            var res = response.Content.ReadAsStringAsync().Result;
            model = JsonSerializer.Deserialize<bool>(res);
        }

        return model;
    }
}