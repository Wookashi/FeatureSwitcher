using System.Collections.Concurrent;
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
    /// <summary>
    /// Name of the named <see cref="HttpClient"/> used to talk to the Node. Register it via
    /// <c>IServiceCollection.AddHttpClient(FeatureManager.NodeHttpClientName, ...)</c> to customize
    /// handler-level behavior (e.g. proxies). Request-level timeout is already enforced independently
    /// of this client's own <see cref="HttpClient.Timeout"/> — see <see cref="_requestTimeout"/>.
    /// </summary>
    internal const string NodeHttpClientName = "FeatureSwitcherNode";

    private readonly List<IFeatureStateModel> _features;
    private readonly string _appName;
    private readonly string _environmentName;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly Uri _nodeAddress;
    private readonly ILogger<FeatureManager> _logger;
    private readonly TimeSpan _requestTimeout;
    private readonly TimeSpan _circuitBreakDuration;
    private long _circuitOpenUntilTicks;
    private volatile bool _isRegistered;
    private volatile bool _isNodeReachable;
    private long _lastNodeContactTicks;

    // Features the Node answered 404 for, so the warning is logged once per feature, not on every read.
    private readonly ConcurrentDictionary<string, byte> _notRegisteredWarned = new(StringComparer.Ordinal);

    /// <summary>
    /// Whether the features have been registered on the Node by this instance. <c>false</c> after a start
    /// without the Node (<c>allowStartWithoutNode</c>) until the background registration succeeds; until
    /// then, features that are new to the Node use their initial states.
    /// </summary>
    public bool IsRegistered => _isRegistered;

    /// <summary>
    /// Whether the last request to the Node (registration or state read) got a response. While
    /// <c>false</c>, feature checks return the last known states — or the initial states if the Node has
    /// not been reached since startup. Updated only by requests, so it reflects the latest feature check.
    /// </summary>
    public bool IsNodeReachable => _isNodeReachable;

    /// <summary>
    /// When the Node last responded to this instance (UTC), or <c>null</c> if it never has.
    /// </summary>
    public DateTimeOffset? LastNodeContactAt
    {
        get
        {
            var ticks = Interlocked.Read(ref _lastNodeContactTicks);
            return ticks == 0 ? null : new DateTimeOffset(ticks, TimeSpan.Zero);
        }
    }

    /// <summary>
    /// Creates a new FeatureManager.
    /// </summary>
    /// <param name="applicationName">Unique name for your application.</param>
    /// <param name="environmentName">Environment name (e.g., "Development", "Production").</param>
    /// <param name="nodeAddress">URI of the Feature Switcher Node service.</param>
    /// <param name="features">List of features to manage.</param>
    /// <param name="httpClientFactory">HTTP client factory for making requests.</param>
    /// <param name="logger">Optional logger. Defaults to NullLogger if not provided.</param>
    /// <param name="requestTimeout">
    /// Maximum time to wait for a single request to the Node before treating it as unreachable.
    /// Defaults to 3 seconds. Enforced independently of the underlying <see cref="HttpClient"/>'s own
    /// timeout, so it applies even if the factory hands back a client with a longer/default timeout.
    /// </param>
    /// <param name="circuitBreakDuration">
    /// After the Node is found unreachable, how long to serve cached state without contacting the Node
    /// again. Defaults to 30 seconds. Prevents a prolonged Node/Manager outage from costing every
    /// <see cref="IsFeatureEnabledAsync(string, CancellationToken)"/> call the full <paramref name="requestTimeout"/>.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when required parameters are null.</exception>
    /// <exception cref="FeatureNameCollisionException">Thrown when feature names are not unique.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the application name or a feature name breaks the required rules of <see cref="NameLimits"/>,
    /// or when the details of a feature (<see cref="IFeatureDetails"/>) exceed <see cref="FeatureDetailsLimits"/>.
    /// Names that only miss the recommended format are logged as a warning.
    /// </exception>
    public FeatureManager(
        string applicationName,
        string environmentName,
        Uri nodeAddress,
        List<IFeatureStateModel> features,
        IHttpClientFactory httpClientFactory,
        ILogger<FeatureManager>? logger = null,
        TimeSpan? requestTimeout = null,
        TimeSpan? circuitBreakDuration = null)
    {
        _appName = applicationName ?? throw new ArgumentNullException(nameof(applicationName));
        _environmentName = environmentName ?? throw new ArgumentNullException(nameof(environmentName));
        _nodeAddress = nodeAddress ?? throw new ArgumentNullException(nameof(nodeAddress));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger ?? NullLogger<FeatureManager>.Instance;
        _requestTimeout = requestTimeout ?? TimeSpan.FromSeconds(3);
        _circuitBreakDuration = circuitBreakDuration ?? TimeSpan.FromSeconds(30);

        if (features is null)
        {
            throw new ArgumentNullException(nameof(features));
        }

        // Names are part of the Node's URLs: reject the ones that cannot work (custom IFeatureStateModel
        // implementations bypass FeatureStateModel's check) and warn about the discouraged ones.
        NameValidator.ValidateRequired("Application", applicationName, nameof(applicationName));
        foreach (var feature in features)
        {
            NameValidator.ValidateRequired("Feature", feature.Name, nameof(features));
        }

        // Validate feature names are unique
        if (features.GroupBy(f => f.Name).Any(g => g.Count() > 1))
        {
            throw new FeatureNameCollisionException("Feature names must be unique.");
        }

        WarnAboutDiscouragedNames(features);

        // FeatureStateModel checks its details when they are set; custom IFeatureDetails implementations
        // are checked here, so an invalid definition fails at startup instead of being sent to the Node.
        foreach (var feature in features)
        {
            if (feature is IFeatureDetails details)
            {
                FeatureDetailsValidator.ValidateDescription(
                    feature.Name, FeatureDetailsValidator.NormalizeDescription(details.Description));
                FeatureDetailsValidator.ValidateLinks(feature.Name, details.Links);
            }
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

    /// <summary>
    /// Checks if a feature is enabled.
    /// Queries the node first; falls back to cached state if unreachable.
    /// </summary>
    /// <param name="featureName">Name of the feature to check.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>True if the feature is enabled, false otherwise.</returns>
    /// <exception cref="FeatureNotRegisteredException">Thrown when the feature was not registered.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled.</exception>
    public async Task<bool> IsFeatureEnabledAsync(string featureName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var feature = _features.FirstOrDefault(f => f.Name == featureName);
        if (feature is null)
        {
            _logger.LogWarning("Feature '{FeatureName}' is not registered", featureName);
            throw new FeatureNotRegisteredException($"Feature '{featureName}' is not registered.");
        }

        if (IsCircuitOpen())
        {
            _logger.LogDebug(
                "Node {NodeAddress} was recently unreachable — serving cached state for feature '{FeatureName}' without contacting the node",
                _nodeAddress, featureName);
            return feature.CurrentLocalState;
        }

        try
        {
            var nodeState = await GetFeatureStateFromNodeAsync(featureName, cancellationToken);
            CloseCircuit();
            if (nodeState is null)
            {
                // The Node is fine, but doesn't know this feature (e.g. registration hasn't succeeded yet,
                // or it was permanently deleted). Only this feature falls back; other features keep
                // reading their current state.
                if (_notRegisteredWarned.TryAdd(featureName, 0))
                {
                    _logger.LogWarning(
                        "Feature '{FeatureName}' is not registered on node {NodeAddress} — using its last known state ({State}) until it is registered",
                        featureName, _nodeAddress, feature.CurrentLocalState);
                }

                return feature.CurrentLocalState;
            }

            _notRegisteredWarned.TryRemove(featureName, out _);
            feature.CurrentLocalState = nodeState.Value;
            _logger.LogDebug("Feature '{FeatureName}' state retrieved from node: {State}", featureName, nodeState);
        }
        catch (NodeUnreachableException)
        {
            _isNodeReachable = false;
            OpenCircuit();
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
    public Task RegisterFeaturesOnNodeAsync(CancellationToken cancellationToken = default)
        => RegisterFeaturesOnNodeAsync(cancellationToken, LogLevel.Error);

    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <param name="transientFailureLevel">
    /// Log level for failures worth retrying (Node unreachable, 5xx). The background registration retries
    /// them and logs its own summary, so it passes a lower level to avoid an error on every attempt.
    /// </param>
    internal async Task RegisterFeaturesOnNodeAsync(CancellationToken cancellationToken, LogLevel transientFailureLevel)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogDebug(
            "Registering {FeatureCount} feature(s) for app '{AppName}' on node {NodeAddress}",
            _features.Count, _appName, _nodeAddress);

        var httpClient = _httpClientFactory.CreateClient(NodeHttpClientName);
        var registrationModel = new AppRegistrationModel
        {
            AppName = _appName,
            Environment = _environmentName,
            Features = _features.Select(CreateRegistrationFeature).ToList(),
        };

        var content = new StringContent(
            JsonSerializer.Serialize(registrationModel),
            Encoding.UTF8,
            "application/json");

        using var cts = CreateRequestCts(cancellationToken);

        HttpResponseMessage response;
        try
        {
            response = await httpClient.PostAsync($"{_nodeAddress}applications", content, cts.Token);
        }
        catch (HttpRequestException ex) when (ex.InnerException is SocketException socketEx)
        {
            _isNodeReachable = false;
            _logger.Log(transientFailureLevel, ex, "Node unreachable at {NodeAddress} during feature registration", _nodeAddress);
            throw new NodeUnreachableException(ex.Message, socketEx.ErrorCode);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _isNodeReachable = false;
            _logger.Log(
                transientFailureLevel,
                "Node unreachable at {NodeAddress} during feature registration (no response within {Timeout})",
                _nodeAddress, _requestTimeout);
            throw new NodeUnreachableException($"Node did not respond within {_requestTimeout}.");
        }

        MarkNodeContact();

        if (response.StatusCode == (HttpStatusCode)422)
        {
            _logger.LogError(
                "Environment mismatch: node environment does not match '{Environment}'",
                _environmentName);
            throw new EnvironmentMismatchException($"Node environment doesn't match '{_environmentName}'.");
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.Log(
                (int)response.StatusCode >= 500 ? transientFailureLevel : LogLevel.Error,
                "Feature registration failed with status {StatusCode}: {Reason}",
                (int)response.StatusCode, response.ReasonPhrase);
            throw new RegistrationException(
                response.ReasonPhrase ?? "Registration failed.",
                (int)response.StatusCode);
        }

        _isRegistered = true;
        _logger.LogInformation(
            "Successfully registered {FeatureCount} feature(s) for app '{AppName}' on node {NodeAddress}",
            _features.Count, _appName, _nodeAddress);
    }

    private void WarnAboutDiscouragedNames(List<IFeatureStateModel> features)
    {
        var discouraged = new List<string>();
        if (NameValidator.RecommendationProblem(_appName) is { } appProblem)
        {
            discouraged.Add($"application '{_appName}' ({appProblem})");
        }

        foreach (var feature in features)
        {
            if (NameValidator.RecommendationProblem(feature.Name) is { } problem)
            {
                discouraged.Add($"feature '{feature.Name}' ({problem})");
            }
        }

        if (discouraged.Count > 0)
        {
            _logger.LogWarning(
                "{Count} name(s) don't follow the recommended format: at most {RecommendedMaxLength} characters, " +
                "ASCII letters, digits, '_', '-' and '.', starting with a letter. They work for now, but the next " +
                "major version of the client will reject them; use shorter names for new features and put details " +
                "in the description. Names: {Names}",
                discouraged.Count, NameLimits.RecommendedMaxLength, string.Join("; ", discouraged));
        }
    }

    private static AppRegistrationModel.RegisterFeatureStateModel CreateRegistrationFeature(IFeatureStateModel feature)
    {
        var model = new AppRegistrationModel.RegisterFeatureStateModel(feature.Name, feature.InitialState);
        if (feature is IFeatureDetails details)
        {
            model.Description = FeatureDetailsValidator.NormalizeDescription(details.Description);
            if (details.Links is { Count: > 0 } links)
            {
                model.Links = links
                    .Select(l => new AppRegistrationModel.RegisterFeatureLinkModel(l.Url.AbsoluteUri, l.Title))
                    .ToList();
            }
        }

        return model;
    }

    private void MarkNodeContact()
    {
        _isNodeReachable = true;
        Interlocked.Exchange(ref _lastNodeContactTicks, DateTime.UtcNow.Ticks);
    }

    /// <summary>
    /// Returns the feature's state on the Node, or <c>null</c> when the Node responded that the feature is
    /// not registered there (404) — the Node itself is fine in that case.
    /// </summary>
    private async Task<bool?> GetFeatureStateFromNodeAsync(string featureName, CancellationToken cancellationToken)
    {
        using var cts = CreateRequestCts(cancellationToken);
        try
        {
            var httpClient = _httpClientFactory.CreateClient(NodeHttpClientName);

            _logger.LogDebug(
                "Querying state of feature '{FeatureName}' for app '{AppName}' from node {NodeAddress}",
                featureName, _appName, _nodeAddress);

            var response = await httpClient.GetAsync(
                $"{_nodeAddress}applications/{Uri.EscapeDataString(_appName)}/features/{Uri.EscapeDataString(featureName)}/state/",
                cts.Token);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                MarkNodeContact();
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Node returned non-success status {StatusCode} for feature '{FeatureName}'",
                    (int)response.StatusCode, featureName);
                throw new NodeUnreachableException(response.ReasonPhrase ?? "Node request failed.");
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            var state = JsonSerializer.Deserialize<bool>(responseBody);
            MarkNodeContact();
            return state;
        }
        catch (HttpRequestException ex) when (ex.InnerException is SocketException socketEx)
        {
            _logger.LogWarning(ex, "Socket exception while querying state of feature '{FeatureName}'", featureName);
            throw new NodeUnreachableException(ex.Message, socketEx.ErrorCode);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                "No response from node within {Timeout} while querying state of feature '{FeatureName}'",
                _requestTimeout, featureName);
            throw new NodeUnreachableException($"Node did not respond within {_requestTimeout}.");
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

    private CancellationTokenSource CreateRequestCts(CancellationToken cancellationToken)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_requestTimeout);
        return cts;
    }

    private bool IsCircuitOpen()
    {
        var openUntilTicks = Interlocked.Read(ref _circuitOpenUntilTicks);
        return openUntilTicks != 0 && DateTime.UtcNow.Ticks < openUntilTicks;
    }

    private void OpenCircuit()
    {
        Interlocked.Exchange(ref _circuitOpenUntilTicks, DateTime.UtcNow.Add(_circuitBreakDuration).Ticks);
    }

    private void CloseCircuit()
    {
        Interlocked.Exchange(ref _circuitOpenUntilTicks, 0);
    }
}
