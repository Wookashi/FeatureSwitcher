# Feature Switcher Client

.NET client SDK for Feature Switcher - a distributed feature flag management system.

## Installation

```bash
dotnet add package Wookashi.FeatureSwitcher.Client.Implementation
```

## Quick Start with Dependency Injection

```csharp
using Wookashi.FeatureSwitcher.Client.Abstraction;
using Wookashi.FeatureSwitcher.Client.Implementation;

// 1. Define configuration
var config = new FeatureSwitcherBasicClientConfiguration(
    applicationName: "MyApp",
    environmentName: "Production",
    nodeAddress: new Uri("http://localhost:8081/"));

// 2. Define your features
var features = new List<IFeatureStateModel>
{
    new MyFeature("DarkMode", initialState: false),
    new MyFeature("NewCheckout", initialState: true),
};

// 3. Register with DI container
services.AddFeatureFlags(config, features);
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
using Wookashi.FeatureSwitcher.Client.Abstraction;
using Wookashi.FeatureSwitcher.Client.Implementation;

// 1. Define your features
var features = new List<IFeatureStateModel>
{
    new MyFeature("DarkMode", initialState: false),
    new MyFeature("NewCheckout", initialState: true),
};

// 2. Configure the client
var serviceProvider = new ServiceCollection().AddHttpClient().BuildServiceProvider();
var httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();

var config = new FeatureSwitcherBasicClientConfiguration(
    applicationName: "MyApp",
    environmentName: "Production",
    nodeAddress: new Uri("http://localhost:8081/"));

// 3. Build and initialize
var featureManager = await new FeatureManagerBuilder(config)
    .AddFeatures(features)
    .AddHttpClientFactory(httpClientFactory!)
    .BuildAsync();

// 4. Check feature states
if (await featureManager.IsFeatureEnabledAsync("DarkMode"))
{
    // Feature is enabled
}
```

## Implementing IFeatureStateModel

Create a class that implements `IFeatureStateModel`:

```csharp
public class MyFeature : IFeatureStateModel
{
    public string Name { get; }
    public bool InitialState { get; }
    public bool CurrentLocalState { get; set; }

    public MyFeature(string name, bool initialState)
    {
        Name = name;
        InitialState = initialState;
        CurrentLocalState = initialState;
    }
}
```

## Configuration

| Property | Description |
|----------|-------------|
| `ApplicationName` | Unique identifier for your application |
| `EnvironmentName` | Environment name (e.g., Development, Production) |
| `NodeAddress` | URI of the Feature Switcher Node service |

## Resilience

The client caches feature states locally. If the Node service becomes unreachable, the client falls back to cached values, ensuring your application continues to function.

## Documentation

For full documentation, visit the [GitHub repository](https://github.com/Wookashi/FeatureSwitcher).

## License

Licensed under the Wookashi.FeatureSwitcher Community License v1.0. See LICENSE.txt for details.
