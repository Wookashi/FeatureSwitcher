using Wookashi.FeatureSwitcher.Client.Abstraction;

namespace Wookashi.FeatureSwitcher.Client.Implementation;

/// <summary>
/// Settings used to connect the application to its Feature Switcher Node; pass it to
/// <c>AddFeatureFlags</c>.
/// </summary>
/// <param name="applicationName">Name under which the application registers on the Node and appears in the Manager.</param>
/// <param name="environmentName">Environment of the application; must match the Node's environment.</param>
/// <param name="nodeAddress">Address of the Feature Switcher Node for this environment.</param>
/// <param name="allowStartWithoutNode">When <c>true</c>, the host starts even if the Node is unreachable during startup registration.</param>
/// <param name="requestTimeout">Maximum time to wait for a single request to the Node. Defaults to 3 seconds.</param>
/// <param name="circuitBreakDuration">How long to serve cached states without contacting an unreachable Node. Defaults to 30 seconds.</param>
/// <exception cref="ArgumentNullException"><paramref name="nodeAddress"/> is null.</exception>
public sealed class FeatureSwitcherBasicClientConfiguration(
    string applicationName,
    string environmentName,
    Uri? nodeAddress,
    bool allowStartWithoutNode = false,
    TimeSpan? requestTimeout = null,
    TimeSpan? circuitBreakDuration = null)
    : IFeatureSwitcherBasicClientConfiguration
{
    /// <inheritdoc />
    public string ApplicationName { get; } = applicationName;

    /// <inheritdoc />
    public string EnvironmentName { get; } = environmentName;

    /// <inheritdoc />
    public Uri NodeAddress { get; } = nodeAddress ?? throw new ArgumentNullException(nameof(nodeAddress));

    /// <inheritdoc />
    public bool AllowStartWithoutNode { get; } = allowStartWithoutNode;

    /// <inheritdoc />
    public TimeSpan RequestTimeout { get; } = requestTimeout ?? TimeSpan.FromSeconds(3);

    /// <inheritdoc />
    public TimeSpan CircuitBreakDuration { get; } = circuitBreakDuration ?? TimeSpan.FromSeconds(30);
}
