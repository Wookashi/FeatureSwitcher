# Feature Switcher — Node

Per-environment feature-flag node for **Feature Switcher**. Deployed close to
your client applications for fast, local feature lookups.

```
Client Apps (SDK) ──HTTP──> Node (this image) ──HTTP──> Manager (central control)
```

Each Node serves a single environment (development, UAT, production, …).
Client apps register their features with the Node and query it for current
state; the Node auto-registers itself with the central Manager on startup.

## Tech
- Runtime: **.NET 10.0** (base `mcr.microsoft.com/dotnet/aspnet:10.0`)
- Storage: Entity Framework Core + **SQLite** (`/data/fs_node.db`)
- Listens on **port 5216** (HTTP)
- Health checks: `GET /health/live` (liveness, used by the image's built-in `HEALTHCHECK`) and
  `GET /health` (full report including Manager connectivity)

## Quick start

```bash
docker run -d \
  --name feature-switcher-node-prod \
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

> The Node authenticates to the Manager with `ManagerSettings__Username` /
> `ManagerSettings__Password`. These must match an existing **Admin** account in
> the Manager (created during the Manager's first-run setup), or self-registration
> will fail until that account exists.

## Configuration

| Variable | Default | Description |
|----------|---------|-------------|
| `NodeConfiguration__Environment` | `testEnv` | Environment this node serves |
| `NodeConfiguration__Name` | `LocalNode` | Display name shown in the Manager UI |
| `NodeConfiguration__Address` | `http://localhost:5216` | Address the Manager uses to reach this node |
| `NodeConfiguration__ConnectionString` | `Data Source=/data/fs_node.db` | SQLite DB path |
| `NodeConfiguration__FeatureStaleAfter` | `30.00:00:00` | Idle time before a flag is marked pending-deletion |
| `NodeConfiguration__FeatureCleanupInterval` | `1.00:00:00` | How often the soft-delete sweep runs |
| `NodeConfiguration__DiagnosticsInterval` | _(not set — disabled)_ | How often memory/GC statistics are logged, e.g. `00:15:00`; not set or `00:00:00` disables |
| `NodeConfiguration__UsageFlushInterval` | `00:01:00` | How often buffered flag usage (last-used time, daily read counts) is written to the database |
| `NodeConfiguration__UsageRetention` | `60.00:00:00` | How long daily read counts are kept; older ones are deleted by the daily sweep. The last read shown in the Manager comes from them, so keep it at least as long as the Manager's `FeatureUsage__UnreadWarningAfter`. `00:00:00` keeps them indefinitely; values below 7 days are raised to 7 |
| `ManagerSettings__Url` | `http://localhost:5033` | URL of the Manager service |
| `ManagerSettings__Username` | _(empty)_ | Manager admin username for self-registration |
| `ManagerSettings__Password` | _(empty)_ | Manager admin password for self-registration |
| `ASPNETCORE_ENVIRONMENT` | `Production` | Hosting environment |
| `TZ` | `Europe/Warsaw` | Timezone |

Mount a volume at **`/data`** to persist the SQLite database (`fs_node.db`). Give each node its own
volume, don't open the database with external tools while the node runs, and back it up with
SQLite's online backup rather than copying the file — another process holding the database lock makes
the node answer `503` ("Database busy"). The image doesn't include the `sqlite3` CLI; use a helper
container mounting the same volume:

```bash
docker run --rm -v fs_node_prod_data:/data -v /backup:/backup alpine \
  sh -c "apk add --no-cache sqlite && sqlite3 /data/fs_node.db '.backup /backup/fs_node.db'"
```
Migrations run automatically on startup.

**Source:** https://github.com/wookashi/FeatureSwitcher
</content>
