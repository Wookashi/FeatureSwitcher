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

## 🤝 Contributing

We welcome contributions! Check out the [Contributing Guide](CONTRIBUTING.md) to get started.
## Authors
* **Lukas Hryciuk** - [Wookashi](https://github.com/Wookashi)


## License
This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details

...to be continued ;)