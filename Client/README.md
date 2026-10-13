# Feature Switcher Client

.NET client SDK for Feature Switcher - a distributed feature flag management system.

## Installation

```bash
dotnet add package Wookashi.FeatureSwitcher.Client.Implementation
```

## Quick Start with Dependency Injection

```csharp
using Wookashi.FeatureSwitcher.Client.Implementation;

// Define your features and register with DI
var config = new FeatureSwitcherBasicClientConfiguration(
    applicationName: "MyApp",
    environmentName: "Production",
    nodeAddress: new Uri("http://localhost:8081/"));

services.AddFeatureFlags(
    config,
    new List<IFeatureStateModel>
    {
        new FeatureStateModel("DarkMode", initialState: false),
        new FeatureStateModel("NewCheckout", initialState: true),
    });
```

Then inject `IFeatureManager` in your classes:

```csharp
public class MyService
{
    private readonly IFeatureManager _featureManager;

    public MyService(IFeatureManager featureManager)
    {
        _featureManager = featureManager;
    }

    public async Task DoSomethingAsync()
    {
        if (await _featureManager.IsFeatureEnabledAsync("DarkMode"))
        {
            // Feature is enabled
        }
    }
}
```

## Manual Initialization (without DI)

```csharp
using Microsoft.Extensions.DependencyInjection;
using Wookashi.FeatureSwitcher.Client.Implementation;

// 1. Set up HttpClientFactory
var serviceProvider = new ServiceCollection().AddHttpClient().BuildServiceProvider();
var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

// 2. Define your features
var features = new List<IFeatureStateModel>
{
    new FeatureStateModel("DarkMode", initialState: false),
    new FeatureStateModel("NewCheckout", initialState: true),
};

// 3. Create and initialize
var featureManager = new FeatureManager(
    applicationName: "MyApp",
    environmentName: "Production",
    nodeAddress: new Uri("http://localhost:8081/"),
    features: features,
    httpClientFactory: httpClientFactory);

await featureManager.RegisterFeaturesOnNodeAsync();

// 4. Check feature states
if (await featureManager.IsFeatureEnabledAsync("DarkMode"))
{
    // Feature is enabled
}
```

## Configuration

`FeatureSwitcherBasicClientConfiguration` parameters:

| Parameter | Description |
|-----------|-------------|
| `applicationName` | Unique identifier for your application |
| `environmentName` | Environment name (e.g., Development, Production) |
| `nodeAddress` | URI of the Feature Switcher Node service |
| `allowStartWithoutNode` | Optional. When `true`, the host starts even if the Node is unreachable during startup registration. Features serve their `initialState` until the Node becomes reachable. Defaults to `false`. |
| `requestTimeout` | Optional. Maximum time to wait for a single request to the Node before treating it as unreachable and falling back to the cached state. Defaults to 3 seconds. |
| `circuitBreakDuration` | Optional. After the Node is found unreachable, how long to keep serving cached state without contacting the Node again. Defaults to 30 seconds. |

`AddFeatureFlags` also takes the list of features to manage.

## Describing features

A feature can carry a description and links (ticket, documentation, runbook, dashboard…). The Manager
shows them when the feature is opened, so whoever switches the flag knows what it does and what
happens when it is turned off. Both are optional and set in code, next to the feature definition:

```csharp
new FeatureStateModel("NewCheckout", initialState: true)
{
    Description = """
        Replaces the legacy multi-step checkout with the single-page flow.

        When OFF:
        - customers are redirected to /checkout/legacy
        - carts stay compatible with both flows
        """,
    Links =
    [
        "https://jira.example.com/browse/SHOP-1423",                // just the URL
        new("Rollout plan", "https://wiki.example.com/rollout"),     // title first, then the URL
        new Uri(settings.DashboardUrl),                             // a Uri works too
    ],
},
```

### Formatting

The description is plain text — HTML is never rendered — with a few conventions that read well both in
code and in the Manager:

| In code | In the Manager |
|---------|----------------|
| a blank line | starts a new paragraph |
| lines starting with `- ` | a bullet list |
| other line breaks | kept as they are |
| `https://…` in the text | a clickable link |

Use a raw string literal (`"""…"""`) for multi-line text: C# removes the indentation, so the description
looks the same in code and in the Manager. Line endings, trailing spaces and repeated blank lines are
normalized, so how the file is edited doesn't matter.

A link without a title is shown as host and path (e.g. `jira.example.com/browse/SHOP-1423`), which is
often enough; add a title when the URL says little (`new("Runbook", "https://…")`).

### Shared flags

Details belong to the application: when several applications register the same flag, each describes
its own use of it, and the Manager shows them all. They are sent with every registration, so changing
or removing them in code updates the Manager on the next start. Different environments can show
different details while they run different versions of the application.

### Limits

| Field | Limit |
|-------|-------|
| Description | 1000 characters after normalization; no control characters other than line breaks and tabs |
| Links | 5 per feature, no duplicate URLs |
| Link URL | absolute `http` or `https`, no credentials (`user:password@`), 2048 characters |
| Link title | optional, single line, 100 characters |

The limits are available as constants in `FeatureDetailsLimits`.

A definition that breaks a limit throws `ArgumentException` as soon as it is created — at application
startup, or in the first test that builds your feature list — with a message saying which feature and
what to fix, for example:

```
Feature 'NewCheckout': description is 1342 characters long, the limit is 1000. Keep the description short and move details to Links.
Link 'jira.example.com/browse/SHOP-1' must be an absolute http or https URL, e.g. "https://jira.example.com/browse/SHOP-1".
```

Custom feature types can provide details by implementing `IFeatureDetails` next to
`IFeatureStateModel`; `FeatureManager` checks them against the same limits when it is created.

Details need a Node and Manager version that supports them; older Nodes accept the registration and
ignore the details.

## Shared Flags

Flags are shared by name on the Node. If two applications register a feature
with the same name, they both reference the same global flag state. Changing the
flag in the Manager from one application affects every application on that Node
that registered the same flag name.

The client does not manually attach or detach applications from flags. The list
passed to `AddFeatureFlags` or `FeatureManager` is the source of truth for the
application-feature links maintained by the Node.

## Lifecycle on the Node

Registration is **append-only** on the Node. New application-feature links are inserted; features missing from the payload are NOT deleted at registration time, so two services that accidentally share an `applicationName` cannot wipe each other's flags.

Every registration and every state read updates `LastUsedAt` on the application-feature link and today's usage counter for the global flag. The Node aggregates these updates in memory and writes them periodically (every minute by default), so usage shown in the Manager can lag slightly.

A background sweep on the Node periodically marks application-feature links whose `LastUsedAt` is older than the configured stale threshold (default 30 days) as `PendingDeletion`. Applications with no active links can also be marked as `PendingDeletion`. Flags in `PendingDeletion`:

- Disappear from the normal `/applications/{app}/features` listing
- Are **auto-restored** if the client reads or re-registers them
- Can be permanently deleted by an admin through the Manager UI (with a 409-on-race guard so a stale dialog cannot delete a freshly-restored flag)

Permanent deletion removes the application-feature link. The global flag is deleted only when no other application still references it.

So from the client's perspective: flags removed from your application code disappear from that application automatically after the threshold passes without any uses. Shared flags still used by other applications keep their global state.

## Resilience

The client caches feature states locally. If the Node service becomes unreachable, the client falls back to cached values, ensuring your application continues to function.

### Tolerating an unreachable Node at startup

By default, `AddFeatureFlags` registers a hosted service that calls `RegisterFeaturesOnNodeAsync` on startup. If the Node is unreachable, registration throws `NodeUnreachableException` and the host fails to start.

Set `allowStartWithoutNode: true` on the configuration to let the application boot anyway. The failure is logged as a warning and features serve their `initialState` until subsequent `IsFeatureEnabledAsync` calls successfully reach the Node:

```csharp
var config = new FeatureSwitcherBasicClientConfiguration(
    applicationName: "MyApp",
    environmentName: "Production",
    nodeAddress: new Uri("http://localhost:8081/"),
    allowStartWithoutNode: true);

services.AddFeatureFlags(config, features);
```

Only `NodeUnreachableException` is suppressed. `EnvironmentMismatchException` and `RegistrationException` (errors returned by a reachable Node) still abort startup because they indicate misconfiguration rather than a transient connectivity issue.

### Bounded requests and circuit breaker

Every request to the Node (registration and feature-state checks) is bounded by `requestTimeout`
(default 3 seconds) regardless of the `HttpClient`'s own timeout, so a Node that stops responding
never blocks your application for longer than that.

If a request to the Node fails or times out, the client opens a circuit breaker for
`circuitBreakDuration` (default 30 seconds): during that window, `IsFeatureEnabledAsync` returns the
cached state immediately without contacting the Node again. This keeps a prolonged Node/Manager
outage from costing every single feature check the full `requestTimeout`. Once the window elapses,
the next check tries the Node again and closes the breaker on success.

```csharp
var config = new FeatureSwitcherBasicClientConfiguration(
    applicationName: "MyApp",
    environmentName: "Production",
    nodeAddress: new Uri("http://localhost:8081/"),
    requestTimeout: TimeSpan.FromSeconds(2),
    circuitBreakDuration: TimeSpan.FromSeconds(15));
```

## Documentation

For full documentation, visit the [GitHub repository](https://github.com/Wookashi/FeatureSwitcher).

## License

Licensed under the Wookashi.FeatureSwitcher Community License v1.0. See LICENSE.txt for details.
