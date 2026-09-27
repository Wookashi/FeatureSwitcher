namespace Wookashi.FeatureSwitcher.Client.Abstraction;

public interface IFeatureSwitcherBasicClientConfiguration
{
    string ApplicationName { get; }
    string EnvironmentName { get; }
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
