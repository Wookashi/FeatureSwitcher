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

## Feature Details

Applications can describe each feature in code — a short description and links such as a ticket,
documentation or a runbook (see [Describing features](./Client/README.md#describing-features) in the
client documentation). Clicking a feature name in the Feature Matrix opens a panel with:

- the description and links registered by that application; when nodes run different versions of the
  application and report different details, a switch shows each node's version,
- the state and usage of the feature on every node,
- for shared flags, the description of every other application that uses the flag.

Hovering a feature name shows the first line of its description. Details are stored per application on
each Node and replaced on every registration, so the application's code stays the source of truth.

## Cleanup candidates

Feature flags tend to stay in code long after they are needed. The Manager points out two kinds of
candidates for removal, under **Filter → Cleanup candidates** in the Feature Matrix and as hints in the
feature details panel:

- **Not read for a period** (default **45 days**, Manager setting `FeatureUsage__UnreadWarningAfter`;
  `00:00:00` disables it). The application registers the flag on a node, but no code has read its state
  there for that long — typically a leftover after a refactoring or a flag that was never wired up.
  Such cells are dimmed, and their tooltip says when the flag was last read. Features registered more
  recently than the period are not reported (they show "not read yet"). A flag read only rarely, e.g. by
  a monthly job, can show up too, so treat it as a hint to check the code.
- **Same state on all nodes**: the flag is present on every node with the same state — ON usually means
  the rollout is complete, OFF that the feature was abandoned. Either way the flag and the unused code
  path can likely be removed.

Reads are counted per application on each node, from the daily read counts, so a node's
`NodeConfiguration__UsageRetention` (default 60 days) should be at least as long as the warning period.
After an upgrade, existing features count as registered at the time of the upgrade, so the first
"not read" warnings appear one warning period later. Nodes older than the Manager don't report reads;
their features are never reported as unread.

## How to run?

> **Full instructions:** see **[docs/RUNNING.md](./docs/RUNNING.md)** for a complete,
> step-by-step guide covering Docker Compose, plain `docker run` (Manager + Node on a
> shared network), local development without Docker, a configuration reference, and
> troubleshooting. The quick start below is the short version.

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

The Node tracks `LastUsedAt` per application-feature link (updated by state reads and registrations) and counts state reads per application and day; registrations are not counted as reads. The Feature Matrix shows the application's last read and its reads in the last 7 days in each cell's tooltip. Daily read counts older than `UsageRetention` (default **60 days**, `NodeConfiguration__UsageRetention`; `00:00:00` keeps them indefinitely, values below 7 days are raised to 7) are deleted by the daily sweep; `LastUsedAt` is not affected. The last read is derived from these counts, so keep the retention at least as long as the Manager's unread warning (see [Cleanup candidates](#cleanup-candidates)).

Flag-state reads don't write to the database: usage is aggregated in memory and written in one transaction every `UsageFlushInterval` (default **60 seconds**, `NodeConfiguration__UsageFlushInterval`), before each soft-delete sweep, and on graceful shutdown. Usage shown in the Manager can therefore lag by up to that interval, and a hard kill of the node loses at most that much usage data; flag states are never affected. A read of a flag that is pending deletion still restores it immediately.

A background sweep marks stale application-feature links, and then applications with no active links, as `PendingDeletion` when their `LastUsedAt` is older than `FeatureStaleAfter` (default **30 days**). Pending items disappear from the normal matrix view, auto-restore when an app reads or re-registers them, and become available for permanent deletion through an Admin-only dialog reachable from the warning-icon badge in the header.

Permanent deletion of a pending flag removes the application-feature link. The global flag row is deleted only when no other application still references it. Permanent deletion is recorded in the Audit Log.

Both knobs are configurable per node via `NodeConfiguration` (`FeatureStaleAfter` and `FeatureCleanupInterval`, default 24h sweep cadence), set in `appsettings.json` or via env vars (e.g., `NodeConfiguration__FeatureStaleAfter=7.00:00:00`).

### Node diagnostics

On startup the Node logs its version, configuration and runtime environment (.NET version, CPU count, GC mode and settings, GC memory limit). Optionally it also logs memory statistics every `DiagnosticsInterval` — **disabled by default** (not set or `00:00:00`); enable it, e.g. `00:15:00`, when investigating memory issues: working set, GC heap size/committed/fragmented memory, an estimate of non-GC (native) memory, GC counts per generation, thread counts and uptime. An entry is raised to a warning when memory approaches the GC memory limit. This makes it possible to tell after an out-of-memory termination whether memory grew steadily or suddenly, and whether in the managed heap or in native memory. Set it via `NodeConfiguration__DiagnosticsInterval`.

Manager availability is logged only when it changes (unavailable / available again after X), not on every `/health` call. Failed registration attempts are logged as warnings while the retry delay ramps up, then once per hour while the Manager stays unavailable.

### Audit Log

Admin actions are recorded to an audit log accessible from the header (audit icon, Admin only). Current actions:
- `ToggleFeature` — feature state changed via the UI, including whether it was shared
- `FeaturePermanentlyDeleted` — application-feature link removed from the pending-deletion dialog; the global flag is deleted only when no other application references it
- `ApplicationPermanentlyDeleted` — application removed from the pending-deletion dialog, including how many feature links and orphaned global flags were removed

Entries older than `AuditLog__Retention` (default **365 days**, format `d.hh:mm:ss`) are deleted at Manager startup and then once a day. Set it to `00:00:00` to keep entries indefinitely.

### Timestamps

All timestamps are stored and returned by the Manager and Node APIs in UTC, as ISO 8601 with the `Z` suffix (e.g. `2026-10-13T18:02:36Z`). The Manager UI shows them in the browser's local time zone. The container time zone (`TZ`) does not affect stored or returned timestamps.

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
