﻿using System.Net.Sockets;
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

    public FeatureManager(IFeatureSwitcherClientConfiguration configuration)    
    {
        _appName = configuration.ApplicationName;
        _environmentName = configuration.EnvironmentName;
        _features = configuration.Features;
        _httpClientFactory = configuration.HttpClientFactory;
        _nodeAddress = configuration.EnvironmentNodeAddress;

        if (_features
            .GroupBy(feature => feature.Name)
            .Any(g => g.Count() > 1))
        {
            throw new FeatureNameCollisionException("Feature names must be unique!");
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

    public async Task RegisterFeaturesOnNode() // TODO change in future releases should be done automaticaly
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
                    .ToList()
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
    //TODO When node is unreachable use last state and throw error after configurable period of time
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