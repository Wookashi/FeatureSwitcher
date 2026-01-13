using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Wookashi.FeatureSwitcher.Client.Abstraction;
using Wookashi.FeatureSwitcher.Client.Abstraction.Exceptions;
using Wookashi.FeatureSwitcher.Client.Implementation.Models;

namespace Wookashi.FeatureSwitcher.Client.Implementation;

public class FeatureManager : IFeatureManager
{
    private readonly List<IFeatureStateModel> _features;
    private readonly string _appName;
    private readonly string _environmentName;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly Uri _nodeAddress;

    internal FeatureManager(FeatureSwitcherFullClientConfiguration configuration)
    {
        _appName = configuration.ApplicationName;
        _environmentName = configuration.EnvironmentName;
        _features = configuration.Features;
        _httpClientFactory = configuration.HttpClientFactory;
        _nodeAddress = configuration.EnvironmentNodeAddress;
    }

    /// <summary>
    /// Checks feature state on node or local storage
    /// </summary>
    /// <param name="featureName">Feature Name</param>
    /// <returns>Feature state</returns>
    /// <exception cref="FeatureNotRegisteredException">Thrown when feature wasn't registered on a start of application</exception>
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
            //TODO Maybe some alert when node is unreachable in some period of time (configurable??)
        }

        if (nodeState is not null)
        {
            collectionFeature.CurrentLocalState = nodeState.Value;
        }

        return collectionFeature.CurrentLocalState;
    }

    internal async Task RegisterFeaturesOnNodeAsync()
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var applicationPackage = new AppRegistrationModel
            {
                AppName = _appName,
                Environment = _environmentName,
                Features = _features.Select(feature =>
                        new AppRegistrationModel.RegisterFeatureStateModel(
                            featureName: feature.Name,
                            initialState: feature.InitialState)
                    )
                    .ToList(),
            };
            var content = new StringContent(JsonSerializer.Serialize(applicationPackage), Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync($"{_nodeAddress}applications", content);

            if (!response.IsSuccessStatusCode)
            {
                throw new RegistrationException(response.ReasonPhrase ?? "Unknown error", (int)response.StatusCode);
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

    }
    
    // TODO Cache default 15s, maybe configurable?
    /// <summary>
    /// Checks is feature enable on node
    /// </summary>
    /// <param name="featureName">Feature Name</param>
    /// <returns>Feature state or exception</returns>
    /// <exception cref="NodeUnreachableException">Returned when node api is unreachable</exception>
    private async Task<bool> IsFeatureEnabledOnNode(string featureName)
    {
        bool model;
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.GetAsync($"{_nodeAddress}applications/{_appName}/features/{featureName}/state/");

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