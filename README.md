# Feature Switcher
Feature Switcher is an simple tool allows you to manage .Net application features in various environments.  


## Project components
Project have three components:

### Client
Nuget package (Wookashi.FeatureSwitcher.Client) should be installed in .Net application to help manage features state.

### Node
Docker container should be placed very close to client apps.

### Manager
User interface used to manipulate features.

## Shared Flags

Flags are grouped by name on each Node. When multiple applications register the
same flag name, they share one global flag state, while each application keeps
its own application-feature link for lifecycle tracking.

The Manager Feature Matrix highlights shared flags with a `Shared` tag and shows
a `Shared` counter in the summary cards. Changing a shared flag from any
application cell updates the global flag state and therefore affects every
application on that Node that registered the same flag name.

## How to run?

Run from the repository root:
```bash
docker-compose up --build
```

### First-Run Setup

On a fresh deployment, the Manager has no user accounts. Nodes will fail to register until the initial setup is completed.

1. Open `http://localhost:8080` in your browser
2. You will be redirected to the **Setup** page
3. Create the initial admin account (for development use `admin` / `admin` to match the default node configuration)
4. Restart the nodes so they can authenticate and register:
   ```bash
   docker-compose restart node-dev node-uat node-prod
   ```
5. Nodes will now appear in the Feature Matrix

### Ports

| Service | URL |
|---------|-----|
| Manager UI | http://localhost:8080 |
| Node (Development) | http://localhost:8081 |
| Node (UAT) | http://localhost:8082 |
| Node (Production) | http://localhost:8083 |

### Node Registration

Nodes authenticate with the Manager on startup using credentials from `ManagerSettings__Username` / `ManagerSettings__Password` environment variables (configured in `docker-compose.yml`). These must match an existing **Admin** account in the Manager database.

If nodes start before setup is completed, they will log an authentication error and skip registration. Restart the nodes after completing setup to trigger registration.

### User Roles

| Role | Manage Users | Register Nodes | Toggle Features | View Features | Scope |
|------|-------------|----------------|-----------------|---------------|-------|
| **Admin** | Yes | Yes | Yes | Yes | All nodes |
| **Editor** | No | No | Yes | Yes | Assigned nodes only |
| **Viewer** | No | No | No | Yes | Assigned nodes only |

### Flag Lifecycle

Registration on the Node is **append-only**: re-registering an application never deletes flags missing from the payload, so two services accidentally sharing an `applicationName` cannot wipe each other's data. The application decides which flags it registers; there are no manual link/unlink actions in the Manager.

The Node tracks `LastUsedAt` per application-feature link and keeps a per-day usage counter per global flag. The Feature Matrix shows the link-level last-used time and the 7-day use count in each cell's tooltip.

A background sweep marks stale application-feature links, and then applications with no active links, as `PendingDeletion` when their `LastUsedAt` is older than `FeatureStaleAfter` (default **30 days**). Pending items disappear from the normal matrix view, auto-restore when an app reads or re-registers them, and become available for permanent deletion through an Admin-only dialog reachable from the warning-icon badge in the header.

Permanent deletion of a pending flag removes the application-feature link. The global flag row is deleted only when no other application still references it. Permanent deletion is recorded in the Audit Log.

Both knobs are configurable per node via `NodeConfiguration` (`FeatureStaleAfter` and `FeatureCleanupInterval`, default 24h sweep cadence), set in `appsettings.json` or via env vars (e.g., `NodeConfiguration__FeatureStaleAfter=7.00:00:00`).

### Audit Log

Admin actions are recorded to an audit log accessible from the header (audit icon, Admin only). Current actions:
- `ToggleFeature` — feature state changed via the UI, including whether it was shared
- `FeaturePermanentlyDeleted` — application-feature link removed from the pending-deletion dialog; the global flag is deleted only when no other application references it
- `ApplicationPermanentlyDeleted` — application removed from the pending-deletion dialog, including how many feature links and orphaned global flags were removed

## Contributing

By submitting a pull request, you agree to the
[Contributor License Agreement](./CLA.md).

If you do not agree with the CLA, please open an issue instead of a PR.

## Authors
* **Lukas Hryciuk** - [Wookashi](https://github.com/Wookashi)


## Licensing

This project is licensed under **Wookashi.FeatureSwitcher – Community License v1.0**.

It is free to use, including commercially, for the versions released under this license.
The author may change licensing terms for **future versions**.

For commercial licensing questions: lukasz.hr@outlook.com
