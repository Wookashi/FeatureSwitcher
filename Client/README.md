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
var features = new List<FeatureStateModel>
{
    new("DarkMode", initialState: false),
    new("NewCheckout", initialState: true),
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

`AddFeatureFlags` also takes the list of features to manage.

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

Every registration and every state read bumps `LastUsedAt` on the application-feature link and upserts today's row in the per-day usage counter for the global flag.

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

## Documentation

For full documentation, visit the [GitHub repository](https://github.com/Wookashi/FeatureSwitcher).

## License

Licensed under the Wookashi.FeatureSwitcher Community License v1.0. See LICENSE.txt for details.
