namespace Wookashi.FeatureSwitcher.Client.Abstraction;

/// <summary>
/// Settings used to connect the application to its Feature Switcher Node.
/// </summary>
public interface IFeatureSwitcherBasicClientConfiguration
{
    /// <summary>
    /// Name under which the application registers on the Node and appears in the Manager.
    /// </summary>
    string ApplicationName { get; }

    /// <summary>
    /// Environment of the application (e.g. "Development", "Production"); must match the Node's environment.
    /// </summary>
    string EnvironmentName { get; }

    /// <summary>
    /// Address of the Feature Switcher Node for this environment.
    /// </summary>
    Uri NodeAddress { get; }

    /// <summary>
    /// When true, the host starts even if the Node is unreachable during startup registration.
    /// Features fall back to their initial/cached states until the Node becomes reachable.
    /// </summary>
    bool AllowStartWithoutNode { get; }

    /// <summary>
    /// Maximum time to wait for a single request to the Node before treating it as unreachable and
    /// falling back to the cached feature state. Keep this short — the Node is expected to run close
    /// to the application, so a slow response is itself a sign it's degraded.
    /// </summary>
    TimeSpan RequestTimeout { get; }

    /// <summary>
    /// After the Node is found unreachable, how long to keep serving cached state without contacting
    /// the Node again. Acts as a simple circuit breaker so a prolonged Node/Manager outage doesn't cost
    /// every feature check the full <see cref="RequestTimeout"/>.
    /// </summary>
    TimeSpan CircuitBreakDuration { get; }
}
