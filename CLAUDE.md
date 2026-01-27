# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build Commands

```bash
# Build entire solution
dotnet build FeatureSwitcher.sln

# Run tests
dotnet test Client/Tests/Client.Implementation.Tests.csproj

# Run single test
dotnet test Client/Tests/Client.Implementation.Tests.csproj --filter "FullyQualifiedName~TestMethodName"

# Run Node API
dotnet run --project Node/Api/Node.Api.csproj

# Run Manager API
dotnet run --project Manager/Api/Manager.Api.csproj

# Run Console demo app
dotnet run --project Console/Console.csproj

# Frontend development (Manager Web)
cd Manager/Web
npm install
npm run dev
```

## Docker

```bash
# Run both services with docker-compose (from repository root)
docker-compose up --build

# Run in detached mode
docker-compose up --build -d

# View logs
docker-compose logs -f

# Stop services
docker-compose down
```

### Port Configuration

| Service | Internal Port | External Port |
|---------|---------------|---------------|
| Manager | 5033 | 8080 |
| Node | 5216 | 8081 |

### Docker Network

Manager connects to Node via internal Docker DNS: `http://node:5216`

After starting containers, register the node:
```bash
curl -X PUT http://localhost:8080/api/nodes \
  -H "Content-Type: application/json" \
  -d '{"nodeName": "DockerNode", "nodeAddress": "http://node:5216"}'
```

## EF Core Migrations

```bash
# Manager migrations (from repository root)
dotnet ef migrations add <MigrationName> --project Manager/Database --startup-project Manager/Api --context NodesDataContext

# Node migrations (from repository root)
dotnet ef migrations add <MigrationName> --project Node/Database --startup-project Node/Api --context FeaturesDataContext
```

## Architecture

Feature Switcher is a distributed feature flag management system with three components:

```
Client Apps ──HTTP──> Node Service ──HTTP──> Manager Service
   (SDK)              (per-env)              (central UI)
```

### Client (NuGet Package)

**Key Classes:**
- `Client/Abstraction/IFeatureManager` - Interface for checking feature states
- `Client/Abstraction/IFeatureStateModel` - Interface for feature state data
- `Client/Implementation/FeatureManager` - Main implementation with HTTP communication and caching
- `Client/Implementation/FeatureStateModel` - Default implementation of `IFeatureStateModel`
- `Client/Implementation/FeatureSwitcherBasicClientConfiguration` - Configuration class
- `Client/Implementation/ConfigureServicesExtensions` - DI registration extension methods

**Usage with Dependency Injection:**
```csharp
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

**Manual Usage:**
```csharp
var featureManager = new FeatureManager(
    applicationName: "MyApp",
    environmentName: "Production",
    nodeAddress: new Uri("http://localhost:8081/"),
    features: features,
    httpClientFactory: httpClientFactory);

await featureManager.RegisterFeaturesOnNodeAsync();

if (await featureManager.IsFeatureEnabledAsync("DarkMode"))
{
    // Feature is enabled
}
```

**CancellationToken Support:**
All async methods support `CancellationToken` for cooperative cancellation:
```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
await featureManager.RegisterFeaturesOnNodeAsync(cts.Token);
var isEnabled = await featureManager.IsFeatureEnabledAsync("DarkMode", cts.Token);
```

### Node (Docker Container - per environment)
- `Node/Api/` - Minimal API endpoints for client registration and feature state queries
- `Node/Database/` - EF Core with `FeaturesDataContext`, stores `ApplicationEntity` and `FeatureEntity`
- Deployed close to client applications, provides fast local feature lookups

### Manager (Central Control)
- `Manager/Api/` - Minimal API that manages node registry and forwards feature updates to nodes
- `Manager/Database/` - EF Core with `NodesDataContext`, stores `NodeEntity` and `StateChangesHistory`
- `Manager/Web/` - React + Vite + Ant Design frontend

### Shared
- `Shared/Abstraction/` - Common models like `FeatureStateModel` and `NodeRegistrationModel`

## Client Exception Hierarchy

All client exceptions inherit from `FeatureSwitcherException`:

| Exception | Code | Description |
|-----------|------|-------------|
| `FeatureSwitcherException` | configurable | Base exception class |
| `FeatureNotRegisteredException` | 1 | Feature was not registered during initialization |
| `EnvironmentMismatchException` | 2 | Application environment doesn't match node environment |
| `NodeUnreachableException` | configurable | Node service cannot be reached |
| `FeatureNameCollisionException` | configurable | Duplicate feature names detected |
| `RegistrationException` | HTTP status code | Registration with node failed |

## API Endpoints

### Manager API (port 8080)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/nodes` | List all registered nodes |
| PUT | `/api/nodes` | Register/update a node |
| GET | `/api/nodes/{nodeId}/applications` | List applications on node |
| GET | `/api/nodes/{nodeId}/applications/{appName}/features` | List features for app |
| PUT | `/api/nodes/{nodeId}/applications/{appName}/features/{featureName}` | Set feature state |
| GET | `/health` | Health check |

### Node API (port 8081)

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/applications` | Register application (client) |
| GET | `/applications` | List applications |
| GET | `/applications/{appName}/features/` | List features for app |
| GET | `/applications/{appName}/features/{featureName}/state/` | Get feature state |
| PUT | `/applications/{appName}/features/{featureName}` | Update feature state |
| GET | `/health` | Health check |

## Frontend (Feature Matrix View)

Located in `Manager/Web/src/views/FeatureMatrix/`:

| File | Description |
|------|-------------|
| `FeatureMatrixPage.tsx` | Main page component with table, toolbar, theme toggle |
| `useFeatureMatrix.ts` | Data fetching hook with progressive loading |
| `types.ts` | TypeScript types (NodeDto, FeatureDto, CellState, etc.) |
| `concurrency.ts` | Concurrency limiter for API calls (max 6 in-flight) |
| `theme.ts` | Day/Night theme hook with localStorage persistence |

Features:
- Progressive loading (nodes → apps → features)
- Sticky columns (Application, Feature)
- Cell states: loading, true (green), false (red), unknown (gray)
- Search filter, "Show only differences" toggle
- Day/Night theme (Ant Design ConfigProvider)
- AbortController for cleanup on refresh/unmount
- Non-blocking error panel

## Client Test Coverage

Tests are located in `Client/Tests/` with 78 tests covering:

| Test File | Tests | Coverage |
|-----------|-------|----------|
| `FeatureManagerTests.cs` | 26 | Constructor validation, IsFeatureEnabledAsync, RegisterFeaturesOnNodeAsync, CancellationToken |
| `FeatureStateModelTests.cs` | 14 | Property initialization, mutation, null handling |
| `FeatureSwitcherBasicClientConfigurationTests.cs` | 12 | Property initialization, null URI validation |
| `ConfigureServicesExtensionsTests.cs` | 9 | DI registration, singleton lifetime, method chaining |
| `ExceptionTests.cs` | 17 | All exception types: message, code, inheritance |

## Key Patterns

- **Direct Construction**: `FeatureManager` has a public constructor for simple instantiation
- **DI Extension Methods**: `AddFeatureFlags()` for easy service registration
- **CancellationToken Support**: All async methods accept optional `CancellationToken` for cooperative cancellation
- **Minimal APIs**: Both Node and Manager use .NET minimal API style
- **In-Memory Option**: Both databases support in-memory mode for testing
- **Resilient Client**: Falls back to cached feature states when Node is unreachable
- **Progressive Loading**: Frontend fetches data incrementally with concurrency control

## Tech Stack

- .NET 10.0 (APIs, Client SDK)
- React 18 + Vite + TypeScript + Ant Design (Manager frontend)
- Entity Framework Core + SQLite
- XUnit + Moq (tests)
- Docker multi-stage builds
