# Running Feature Switcher

This guide walks you through running the full Feature Switcher stack from scratch.
If you follow it top to bottom you will end up with a working Manager UI and one or
more Nodes registered to it.

There are three ways to run the stack, in order of how easy they are:

1. **[Docker Compose](#1-docker-compose-recommended)** — one command, brings up the Manager and three Nodes. Start here.
2. **[Plain `docker run`](#2-plain-docker-run-manager--one-node)** — run the published images by hand on a shared network. Good for understanding the moving parts or a custom deployment.
3. **[Local development (no Docker)](#3-local-development-without-docker)** — run the APIs and the web UI from source with the .NET SDK and Node.js.

---

## Architecture in one picture

```
Client Apps (SDK) ──HTTP──> Node (per-environment) ──HTTP──> Manager (central UI + API)
```

- **Manager** — central control plane. Hosts the web UI and REST API, keeps the node registry, manages users (JWT + roles), forwards feature toggles to nodes. Listens on **port 5033**.
- **Node** — one per environment (development, UAT, production…). Client apps register feature flags with it and read their state from it. Each node self-registers with the Manager on startup. Listens on **port 5216**.
- **Client** — the `Wookashi.FeatureSwitcher.Client` NuGet package your .NET app uses to talk to a Node. Not a container.

Both Manager and Node store their data in **SQLite** under `/data`.

---

## 1. Docker Compose (recommended)

### Prerequisites

- [Docker](https://docs.docker.com/get-docker/) and Docker Compose (bundled with Docker Desktop)

That's it — you do **not** need the .NET SDK or Node.js for this path; everything is built inside the containers.

### Step 1 — Start the stack

From the repository root:

```bash
docker-compose up --build
```

This builds and starts four containers:

| Service | Container | URL |
|---------|-----------|-----|
| Manager UI + API | `manager` | http://localhost:8080 |
| Node — Development | `node-dev` | http://localhost:8081 |
| Node — UAT | `node-uat` | http://localhost:8082 |
| Node — Production | `node-prod` | http://localhost:8083 |

Add `-d` to run detached (in the background):

```bash
docker-compose up --build -d
```

> **Expected on first run:** the nodes start before any user exists in the Manager,
> so they **fail to register** and log an authentication error. This is normal —
> you fix it in the next step.

### Step 2 — Create the first admin account

The Manager ships with **no users**. On first launch it redirects you to a setup page.

1. Open **http://localhost:8080** — you'll land on the **Setup** page.
2. Create the initial **Admin** account.
   For local development use **`admin` / `admin`** so it matches the node credentials
   baked into `docker-compose.yml` (`ManagerSettings__Username` / `ManagerSettings__Password`).

### Step 3 — Restart the nodes so they register

Now that an admin exists, restart the nodes so they can authenticate and self-register:

```bash
docker-compose restart node-dev node-uat node-prod
```

Refresh the Manager UI — the three nodes now appear in the Feature Matrix. Done.

### Everyday commands

```bash
# View logs (all services, follow)
docker-compose logs -f

# Logs for one service
docker-compose logs -f manager

# Stop everything (keeps data volumes)
docker-compose down

# Stop and wipe all data (deletes the SQLite volumes)
docker-compose down -v
```

### Where is the data?

Each service persists its SQLite database in a named Docker volume:

| Volume | Contents |
|--------|----------|
| `fs_manager_data` | Manager DB (`/data/fs_manager.db`) — users, nodes, audit log |
| `fs_node_dev_data` | Dev node DB (`/data/fs_node.db`) |
| `fs_node_uat_data` | UAT node DB |
| `fs_node_prod_data` | Prod node DB |

These survive `docker-compose down`. Use `docker-compose down -v` to remove them.

---

## 2. Plain `docker run` (Manager + one Node)

Use this if you want to run the published images directly, e.g. on a server, without
the repo's compose file. The key is that the Node must be able to reach the Manager
by name, so we put both on a shared Docker network.

Images on Docker Hub:
- `wookashi123/featureswitcher-manager`
- `wookashi123/featureswitcher-node`

### Step 1 — Create a network

```bash
docker network create featureswitcher
```

### Step 2 — Start the Manager

```bash
docker run -d \
  --name manager \
  --network featureswitcher \
  -p 8080:5033 \
  -v fs_manager_data:/data \
  -e Jwt__SecretKey="ChangeMe_AtLeast32CharactersLong!!!!" \
  wookashi123/featureswitcher-manager:latest
```

> `Jwt__SecretKey` **must** be set and at least 32 characters — it signs the
> auth tokens. There is no default.

### Step 3 — Create the first admin

Open **http://localhost:8080**, complete the **Setup** page, and create an Admin
account. Pick credentials you'll reuse for the node below (e.g. `admin` / `admin`).

### Step 4 — Start a Node

The node authenticates to the Manager with the admin credentials you just created,
then self-registers. Note `ManagerSettings__Url` uses the **container name** `manager`
(resolvable because both are on the `featureswitcher` network):

```bash
docker run -d \
  --name node-prod \
  --network featureswitcher \
  -p 8081:5216 \
  -v fs_node_prod_data:/data \
  -e NodeConfiguration__Environment="production" \
  -e NodeConfiguration__Name="Production" \
  -e NodeConfiguration__Address="http://node-prod:5216" \
  -e ManagerSettings__Url="http://manager:5033" \
  -e ManagerSettings__Username="admin" \
  -e ManagerSettings__Password="admin" \
  wookashi123/featureswitcher-node:latest
```

Refresh the Manager UI — the node appears once registration succeeds. If you started
the node **before** creating the admin, just restart it: `docker restart node-prod`.

Repeat Step 4 with different `--name`, `-p`, `NodeConfiguration__Environment/Name/Address`,
and volume to add more environments.

---

## 3. Local development (without Docker)

Run the pieces from source when you're developing.

### Prerequisites

- [.NET SDK 10.0](https://dotnet.microsoft.com/download)
- [Node.js 22+](https://nodejs.org/) (for the Manager web UI)

> **Private NuGet feed note:** restore may try to reach a private feed
> (`nuget.int.cuk.pl`) and fail if it's unreachable. Skip the vulnerability audit with
> `dotnet restore -p:NuGetAudit=false` if you hit this.

### Build and test

```bash
# Build the whole solution
dotnet build FeatureSwitcher.sln

# Run the client SDK tests
dotnet test Client/Tests/Client.Implementation.Tests.csproj
```

### Run the Manager API

```bash
dotnet run --project Manager/Api/Manager.Api.csproj
```

In development the Manager uses a dev-only JWT secret from
`Manager/Api/appsettings.Development.json`, so you don't have to set `Jwt__SecretKey`.

### Run the Manager web UI

In a second terminal:

```bash
cd Manager/Web
npm install
npm run dev
```

Vite serves the UI (default http://localhost:5173) and proxies API calls to the
Manager. Open it, complete the Setup page, and create an admin.

### Run a Node API

In a third terminal:

```bash
dotnet run --project Node/Api/Node.Api.csproj
```

Configure it via `Node/Api/appsettings.Development.json` or environment variables
(`NodeConfiguration__*`, `ManagerSettings__*`). Make sure `ManagerSettings__Url`
points at your running Manager and `ManagerSettings__Username/Password` match the
admin you created.

### Try the console demo

A sample client app lives in `Console/`:

```bash
dotnet run --project Console/Console.csproj
```

---

## Configuration reference

### Manager

| Variable | Default | Description |
|----------|---------|-------------|
| `Jwt__SecretKey` | _(empty — **required**)_ | HMAC signing key for JWTs, **≥ 32 chars** |
| `Jwt__Issuer` | `FeatureSwitcher` | JWT issuer |
| `Jwt__Audience` | `FeatureSwitcher` | JWT audience |
| `Jwt__ExpirationMinutes` | `60` | Token lifetime in minutes |
| `Database__ConnectionString` | `Data Source=/data/fs_manager.db` | SQLite DB path |
| `ASPNETCORE_ENVIRONMENT` | `Production` | Hosting environment |
| `TZ` | `Europe/Warsaw` | Timezone |

Listens on **5033** (HTTP). Mount a volume at **`/data`** to persist the database.

### Node

| Variable | Default | Description |
|----------|---------|-------------|
| `NodeConfiguration__Environment` | `testEnv` | Environment this node serves |
| `NodeConfiguration__Name` | `LocalNode` | Display name shown in the Manager UI |
| `NodeConfiguration__Address` | `http://localhost:5216` | Address the Manager uses to reach this node |
| `NodeConfiguration__ConnectionString` | `Data Source=/data/fs_node.db` | SQLite DB path |
| `NodeConfiguration__FeatureStaleAfter` | `30.00:00:00` | Idle time (`d.hh:mm:ss`) before a flag is marked pending-deletion |
| `NodeConfiguration__FeatureCleanupInterval` | `1.00:00:00` | How often the soft-delete sweep runs |
| `ManagerSettings__Url` | `http://localhost:5033` | URL of the Manager service |
| `ManagerSettings__Username` | _(empty)_ | Manager admin username, for self-registration |
| `ManagerSettings__Password` | _(empty)_ | Manager admin password, for self-registration |
| `ASPNETCORE_ENVIRONMENT` | `Production` | Hosting environment |
| `TZ` | `Europe/Warsaw` | Timezone |

Listens on **5216** (HTTP), with a built-in health check at `GET /health`.
Mount a volume at **`/data`** to persist the database.

> Configuration keys use the .NET double-underscore convention: `Section__Key`
> maps to the `Section:Key` entry in `appsettings.json`.

---

## Troubleshooting

**Nodes don't show up in the Manager UI.**
The most common cause is that they started before the admin account existed, or their
credentials don't match. Confirm an admin exists (you completed Setup), confirm the
node's `ManagerSettings__Username/Password` match that admin, then restart the node.
Check the node logs (`docker-compose logs node-dev` or `docker logs node-prod`) for an
authentication error.

**Node logs "authentication failed" / 401 on startup.**
`ManagerSettings__Username` / `ManagerSettings__Password` don't match an existing Admin
account in the Manager. Fix the credentials (or the admin), then restart the node.

**Manager fails to start with a JWT/secret error.**
`Jwt__SecretKey` is missing or shorter than 32 characters. Set it to a long random
string. (Not needed when running locally in the Development environment, which uses a
dev secret.)

**Node can't reach the Manager (connection refused).**
With `docker run`, both containers must be on the same network and `ManagerSettings__Url`
must use the Manager's **container name** (`http://manager:5033`), not `localhost`.
`localhost` inside a container refers to that container, not the host.

**I want to start completely fresh.**
`docker-compose down -v` removes the data volumes, wiping users, nodes, and flags. The
next `up` will send you back through Setup.

**Restore fails reaching `nuget.int.cuk.pl`.**
That's a private feed. Run restores with `dotnet restore -p:NuGetAudit=false`.

---

## Related docs

- [`README.md`](../README.md) — project overview, roles, flag lifecycle, audit log
- [`Manager/DOCKERHUB.md`](../Manager/DOCKERHUB.md) — Manager image Docker Hub overview
- [`Node/DOCKERHUB.md`](../Node/DOCKERHUB.md) — Node image Docker Hub overview
- [`.claude/CLAUDE.md`](../.claude/CLAUDE.md) — full architecture, API endpoints, and internals
</content>
</invoke>
