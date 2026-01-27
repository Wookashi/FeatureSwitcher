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
services.AddFeatureFlags(
    applicationName: "MyApp",
    environmentName: "Production",
    nodeAddress: new Uri("http://localhost:8081/"),
    features: new List<FeatureStateModel>
    {
        new("DarkMode", initialState: false),
        new("NewCheckout", initialState: true),
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

| Parameter | Description |
|-----------|-------------|
| `applicationName` | Unique identifier for your application |
| `environmentName` | Environment name (e.g., Development, Production) |
| `nodeAddress` | URI of the Feature Switcher Node service |
| `features` | List of features to manage |

## Resilience

The client caches feature states locally. If the Node service becomes unreachable, the client falls back to cached values, ensuring your application continues to function.

## Documentation

For full documentation, visit the [GitHub repository](https://github.com/Wookashi/FeatureSwitcher).

## License

Licensed under the Wookashi.FeatureSwitcher Community License v1.0. See LICENSE.txt for details.
