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

Nodes auto-register with the Manager on startup using configured credentials. Manual registration (requires JWT):
```bash
# Get token
TOKEN=$(curl -s -X POST http://localhost:8080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin"}' | jq -r .token)

# Register node
curl -X PUT http://localhost:8080/api/nodes \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
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

## Authentication (JWT)

The Manager API uses JWT-based authorization. All `/api/*` endpoints require a valid Bearer token except `/api/auth/login` and `/health`.

### Configuration

- **Manager**: `Jwt` section (SecretKey, Issuer, Audience, ExpirationMinutes) and `AdminCredentials` section (Username, Password) in appsettings
- **Node**: `ManagerSettings` section includes `Username` and `Password` for authenticating with Manager during self-registration
- **Development defaults**: username `admin`, password `admin`, dev-only secret key in `appsettings.Development.json`

### Key Files

| File | Description |
|------|-------------|
| `Manager/Api/Configuration/JwtSettings.cs` | JWT configuration model |
| `Manager/Api/Configuration/AdminCredentials.cs` | Admin credentials model |
| `Manager/Api/Services/AuthService.cs` | Credential validation + JWT token generation (HMAC-SHA256) |
| `Manager/Api/Models/LoginRequest.cs` | Login request DTO |
| `Manager/Api/Models/LoginResponse.cs` | Login response DTO (Token, ExpiresAt) |
| `Manager/Web/src/auth/authToken.ts` | Token storage in localStorage + expiration check |
| `Manager/Web/src/auth/authFetch.ts` | Fetch wrapper adding Bearer header, redirects to `/login` on 401 |
| `Manager/Web/src/auth/RequireAuth.tsx` | Route guard redirecting to `/login` if no valid token |
| `Manager/Web/src/views/Login/LoginPage.tsx` | Login page (Ant Design form) |

### Auth Flow

1. User/Node calls `POST /api/auth/login` with `{ username, password }` to get a JWT
2. Token is included as `Authorization: Bearer {token}` header on subsequent requests
3. Frontend stores token in localStorage, `authFetch()` wrapper auto-attaches it
4. On 401 response, frontend clears token and redirects to `/login`
5. Nodes authenticate with Manager before self-registering (if credentials configured)

### Docker Environment Variables

- Manager: `Jwt__SecretKey`, `AdminCredentials__Username`, `AdminCredentials__Password`
- Nodes: `ManagerSettings__Username`, `ManagerSettings__Password`

## API Endpoints

### Manager API (port 8080)

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/api/auth/login` | No | Authenticate and obtain JWT |
| GET | `/api/nodes` | Yes | List all registered nodes |
| PUT | `/api/nodes` | Yes | Register/update a node |
| GET | `/api/nodes/{nodeId}/applications` | Yes | List applications on node |
| GET | `/api/nodes/{nodeId}/applications/{appName}/features` | Yes | List features for app |
| PUT | `/api/nodes/{nodeId}/applications/{appName}/features/{featureName}` | Yes | Set feature state |
| GET | `/health` | No | Health check |

### Node API (port 8081)

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/applications` | Register application (client) |
| GET | `/applications` | List applications |
| GET | `/applications/{appName}/features/` | List features for app |
| GET | `/applications/{appName}/features/{featureName}/state/` | Get feature state |
| PUT | `/applications/{appName}/features/{featureName}` | Update feature state |
| GET | `/health` | Health check |

## Frontend (Manager Web)

Uses React Router (`react-router-dom` v6) for client-side routing:
- `/login` — Login page (public)
- `/` — Feature Matrix page (requires valid JWT)
- `*` — Redirects to `/`

All API calls use `authFetch()` from `Manager/Web/src/auth/` which auto-attaches JWT and handles 401 redirects.

### Feature Matrix View

Located in `Manager/Web/src/views/FeatureMatrix/`:

| File | Description |
|------|-------------|
| `FeatureMatrixPage.tsx` | Main page component with table, toolbar, theme toggle, logout button |
| `useFeatureMatrix.ts` | Data fetching hook with progressive loading (uses `authFetch`) |
| `types.ts` | TypeScript types (NodeDto, FeatureDto, CellState, etc.) |
| `concurrency.ts` | Concurrency limiter for API calls (max 6 in-flight) |
| `theme.ts` | Day/Night theme hook with localStorage persistence |

Features:
- Progressive loading (nodes → apps → features)
- Sticky columns (Application, Feature)
- Cell states: loading, true (green), false (red), unknown/N/A (gray)
- Search filter, "Show only differences" toggle
- Day/Night theme (Ant Design ConfigProvider)
- AbortController for cleanup on refresh/unmount
- Non-blocking error panel
- Logout button in header

### Cell State Types

```typescript
type CellState =
  | { kind: 'loading' }
  | { kind: 'value'; value: boolean }
  | { kind: 'unknown'; reason?: string };
```

### Cell State Resolution

Cells show "N/A" (unknown state) in these scenarios:
- **Node unreachable**: Node failed to respond during fetch (tooltip: "Node unreachable: [error]")
- **Feature not present**: Feature exists on other nodes but not on this node (tooltip: "Feature not present on this node")
- **Toggle failed**: Feature state toggle request failed (tooltip: error message)
- **Timeout**: Request exceeded 10-second timeout

### Fetch Timeout Configuration

All API fetch calls in `useFeatureMatrix.ts` have a 10-second timeout using `AbortSignal.timeout(10000)`:
- `fetchNodes()` - fetching node list
- `fetchApplications()` - fetching apps per node
- `fetchFeatures()` - fetching features per app
- `toggleFeatureState()` - toggling feature state

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
- **JWT Authorization**: Manager API endpoints protected with Bearer tokens; frontend uses `authFetch()` wrapper
- **Node Self-Registration**: Nodes authenticate with Manager and auto-register on startup via `ManagerRegistrationHostedService`

## Tech Stack

- .NET 10.0 (APIs, Client SDK)
- React 18 + Vite + TypeScript + Ant Design + React Router (Manager frontend)
- Entity Framework Core + SQLite
- JWT (Microsoft.AspNetCore.Authentication.JwtBearer) for Manager API auth
- XUnit + Moq (tests)
- Docker multi-stage builds

## Build Notes

- NuGet restore may fail if private feed (`nuget.int.cuk.pl`) is unreachable. Use `dotnet restore -p:NuGetAudit=false` to skip vulnerability audit.
- Microsoft.OpenApi v2.3.0 (transitive via Swashbuckle 10.1.0) uses root `Microsoft.OpenApi` namespace — not `Microsoft.OpenApi.Models`. Security scheme references use `OpenApiSecuritySchemeReference` class.
