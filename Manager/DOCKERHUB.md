# Feature Switcher — Manager

Central control plane for **Feature Switcher**, a distributed feature-flag management system.

```
Client Apps ──HTTP──> Node (per-environment) ──HTTP──> Manager (this image)
```

The Manager hosts the web UI and REST API: it keeps the registry of Node
instances, forwards feature-state changes to the right Node, manages users
(JWT + role-based access), and records an audit log.

## Features
- React 18 + Vite + Ant Design web UI (served from the same container)
- REST API for nodes, applications, and feature state
- JWT auth with RBAC (Admin / Editor / Viewer) and per-node access control
- Audit logging and pending-deletion review for soft-deleted flags

## Tech
- Runtime: **.NET 10.0** (base `mcr.microsoft.com/dotnet/aspnet:10.0`)
- Storage: Entity Framework Core + **SQLite**
- Listens on **port 5033** (HTTP)

## Quick start

```bash
docker run -d \
  --name feature-switcher-manager \
  -p 8080:5033 \
  -v fs_manager_data:/data \
  -e Jwt__SecretKey="ChangeMe_AtLeast32CharactersLong!!!!" \
  wookashi123/featureswitcher-manager:latest
```

Open <http://localhost:8080>.

### First-run setup
On first launch there are no users, so you're redirected to **`/setup`** to create
the initial **Admin** account. The credentials you create here must match the
`ManagerSettings__Username` / `ManagerSettings__Password` configured on your Nodes,
otherwise the nodes can't authenticate to self-register.

## Configuration

| Variable | Default | Description |
|----------|---------|-------------|
| `Jwt__SecretKey` | _(empty — must be set)_ | HMAC signing key, **≥ 32 chars** |
| `Jwt__Issuer` | `FeatureSwitcher` | JWT issuer |
| `Jwt__Audience` | `FeatureSwitcher` | JWT audience |
| `Jwt__ExpirationMinutes` | `60` | Token lifetime |
| `Database__ConnectionString` | `Data Source=/data/fs_manager.db` | SQLite DB path |
| `AuditLog__Retention` | `365.00:00:00` | Audit log retention (`d.hh:mm:ss`); older entries are deleted at startup and daily. `00:00:00` keeps them indefinitely |
| `ASPNETCORE_ENVIRONMENT` | `Production` | Hosting environment |
| `TZ` | `Europe/Warsaw` | Timezone |

Mount a volume at **`/data`** to persist the SQLite database (`fs_manager.db`).
Migrations run automatically on startup.

**Source:** https://github.com/wookashi/FeatureSwitcher
</content>
