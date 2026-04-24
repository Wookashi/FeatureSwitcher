using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Wookashi.FeatureSwitcher.Client.Abstraction;
using Wookashi.FeatureSwitcher.Client.Abstraction.Exceptions;
using Wookashi.FeatureSwitcher.Client.Abstraction.Models;
using Wookashi.FeatureSwitcher.Client.Implementation.Models;

namespace Wookashi.FeatureSwitcher.Client.Implementation;

/// <summary>
/// Manages feature flags for your application.
/// Communicates with a Feature Switcher Node to get feature states.
/// Falls back to cached local states when the node is unreachable.
/// </summary>
public class FeatureManager : IFeatureManager
{
    private readonly List<IFeatureStateModel> _features;
    private readonly string _appName;
    private readonly string _environmentName;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly Uri _nodeAddress;
    private readonly ILogger<FeatureManager> _logger;

    /// <summary>
    /// Creates a new FeatureManager.
    /// </summary>
    /// <param name="applicationName">Unique name for your application.</param>
    /// <param name="environmentName">Environment name (e.g., "Development", "Production").</param>
    /// <param name="nodeAddress">URI of the Feature Switcher Node service.</param>
    /// <param name="features">List of features to manage.</param>
    /// <param name="httpClientFactory">HTTP client factory for making requests.</param>
    /// <param name="logger">Optional logger. Defaults to NullLogger if not provided.</param>
    /// <exception cref="ArgumentNullException">Thrown when required parameters are null.</exception>
    /// <exception cref="FeatureNameCollisionException">Thrown when feature names are not unique.</exception>
    public FeatureManager(
        string applicationName,
        string environmentName,
        Uri nodeAddress,
        List<IFeatureStateModel> features,
        IHttpClientFactory httpClientFactory,
        ILogger<FeatureManager>? logger = null)
    {
        _appName = applicationName ?? throw new ArgumentNullException(nameof(applicationName));
        _environmentName = environmentName ?? throw new ArgumentNullException(nameof(environmentName));
        _nodeAddress = nodeAddress ?? throw new ArgumentNullException(nameof(nodeAddress));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger ?? NullLogger<FeatureManager>.Instance;

        if (features is null)
        {
            throw new ArgumentNullException(nameof(features));
        }

        // Validate feature names are unique
        if (features.GroupBy(f => f.Name).Any(g => g.Count() > 1))
        {
            throw new FeatureNameCollisionException("Feature names must be unique.");
        }

        _features = features;

        _logger.LogInformation(
            "FeatureManager initialized for app '{AppName}' in environment '{Environment}' targeting node {NodeAddress} with {FeatureCount} feature(s)",
            _appName, _environmentName, _nodeAddress, _features.Count);
    }

    /// <summary>
    /// Checks if a feature is enabled.
    /// Queries the node first; falls back to cached state if unreachable.
    /// </summary>
    /// <param name="feature">Feature to check.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>True if the feature is enabled, false otherwise.</returns>
    /// <exception cref="FeatureNotRegisteredException">Thrown when the feature was not registered.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled.</exception>
    public Task<bool> IsFeatureEnabledAsync(IFeatureStateModel feature, CancellationToken cancellationToken = default)
    {
        if (feature is null)
        {
            throw new ArgumentNullException(nameof(feature));
        }

        return IsFeatureEnabledAsync(feature.Name, cancellationToken);
    }

    public async Task<bool> IsFeatureEnabledAsync(string featureName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var feature = _features.FirstOrDefault(f => f.Name == featureName);
        if (feature is null)
        {
            _logger.LogWarning("Feature '{FeatureName}' is not registered", featureName);
            throw new FeatureNotRegisteredException($"Feature '{featureName}' is not registered.");
        }

        try
        {
            var nodeState = await GetFeatureStateFromNodeAsync(featureName, cancellationToken);
            feature.CurrentLocalState = nodeState;
            _logger.LogDebug("Feature '{FeatureName}' state retrieved from node: {State}", featureName, nodeState);
        }
        catch (NodeUnreachableException)
        {
            _logger.LogWarning(
                "Node unreachable — falling back to cached state for feature '{FeatureName}': {CachedState}",
                featureName, feature.CurrentLocalState);
        }

        return feature.CurrentLocalState;
    }

    /// <summary>
    /// Registers all features with the node.
    /// Call this once during application startup.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <exception cref="NodeUnreachableException">Thrown when the node cannot be reached.</exception>
    /// <exception cref="EnvironmentMismatchException">Thrown when environments don't match.</exception>
    /// <exception cref="RegistrationException">Thrown when registration fails.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled.</exception>
    public async Task RegisterFeaturesOnNodeAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogDebug(
            "Registering {FeatureCount} feature(s) for app '{AppName}' on node {NodeAddress}",
            _features.Count, _appName, _nodeAddress);

        var httpClient = _httpClientFactory.CreateClient();
        var registrationModel = new AppRegistrationModel
        {
            AppName = _appName,
            Environment = _environmentName,
            Features = _features
                .Select(f => new AppRegistrationModel.RegisterFeatureStateModel(f.Name, f.InitialState))
                .ToList(),
        };

        var content = new StringContent(
            JsonSerializer.Serialize(registrationModel),
            Encoding.UTF8,
            "application/json");

        HttpResponseMessage response;
        try
        {
            response = await httpClient.PostAsync($"{_nodeAddress}applications", content, cancellationToken);
        }
        catch (HttpRequestException ex) when (ex.InnerException is SocketException socketEx)
        {
            _logger.LogError(ex, "Node unreachable at {NodeAddress} during feature registration", _nodeAddress);
            throw new NodeUnreachableException(ex.Message, socketEx.ErrorCode);
        }

        if (response.StatusCode == (HttpStatusCode)422)
        {
            _logger.LogError(
                "Environment mismatch: node environment does not match '{Environment}'",
                _environmentName);
            throw new EnvironmentMismatchException($"Node environment doesn't match '{_environmentName}'.");
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Feature registration failed with status {StatusCode}: {Reason}",
                (int)response.StatusCode, response.ReasonPhrase);
            throw new RegistrationException(
                response.ReasonPhrase ?? "Registration failed.",
                (int)response.StatusCode);
        }

        _logger.LogInformation(
            "Successfully registered {FeatureCount} feature(s) for app '{AppName}' on node {NodeAddress}",
            _features.Count, _appName, _nodeAddress);
    }

    private async Task<bool> GetFeatureStateFromNodeAsync(string featureName, CancellationToken cancellationToken)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();

            _logger.LogDebug(
                "Querying state of feature '{FeatureName}' for app '{AppName}' from node {NodeAddress}",
                featureName, _appName, _nodeAddress);

            var response = await httpClient.GetAsync(
                $"{_nodeAddress}applications/{_appName}/features/{featureName}/state/",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Node returned non-success status {StatusCode} for feature '{FeatureName}'",
                    (int)response.StatusCode, featureName);
                throw new NodeUnreachableException(response.ReasonPhrase ?? "Node request failed.");
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<bool>(responseBody);
        }
        catch (HttpRequestException ex) when (ex.InnerException is SocketException socketEx)
        {
            _logger.LogWarning(ex, "Socket exception while querying state of feature '{FeatureName}'", featureName);
            throw new NodeUnreachableException(ex.Message, socketEx.ErrorCode);
        }
        catch (NodeUnreachableException)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unexpected error while querying state of feature '{FeatureName}'", featureName);
            throw new NodeUnreachableException(ex.Message);
        }
    }
}
