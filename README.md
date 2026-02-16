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
