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

run from folder FeatureSwitcher (main in repository)
   ```bash
   docker-compose up --build
   ```

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
