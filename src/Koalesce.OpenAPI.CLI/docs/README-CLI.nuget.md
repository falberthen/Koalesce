# Koalesce CLI for OpenAPI


![Koalesce CLI](https://raw.githubusercontent.com/falberthen/Koalesce/master/img/cli_small.png)

The `Koalesce.OpenAPI.CLI` is a standalone tool that uses [Koalesce.OpenAPI](https://github.com/falberthen/Koalesce#readme) to merge OpenAPI definitions directly into a file in the disk.

> **Official packages are published exclusively to [NuGet.org](https://www.nuget.org/packages?q=Koalesce) by the maintainer.** Do not trust packages from unofficial sources.

---

## Quick Start

### 1. Install Koalesce CLI

Install globally to merge OpenAPI specs without hosting an app:

```bash
dotnet tool install --global Koalesce.OpenAPI.CLI --prerelease
```

### 2. Use Koalesce CLI for OpenAPI

```bash
koalesce --config ./appsettings.json --output ./gateway.yaml --verbose
```

<br/>

<img src="https://raw.githubusercontent.com/falberthen/Koalesce/master/img/Screenshot_CLI.png"/>

#### Arguments:

- 🔺 `--config` - Path to your `appsettings.json`
- 🔺 `--output` - Path for the merged OpenAPI spec file
- `--verbose` - Enable detailed logging
- `--version` - Display current version

<br/>

> 💡 **Note:** The CLI uses the same configuration model as the Middleware. All settings are defined in `appsettings.json`.

---

## Documentation & Links

- [Full Documentation](https://github.com/falberthen/Koalesce#readme)
- [Sample Projects](https://github.com/falberthen/Koalesce/tree/master/samples)
- [Changelog](https://github.com/falberthen/Koalesce/blob/master/CHANGELOG.md)
- [CLI Changelog](https://github.com/falberthen/Koalesce/tree/master/src/Koalesce.OpenAPI.CLI/CHANGELOG.md)
- [Contributing](https://github.com/falberthen/Koalesce/blob/master/CONTRIBUTING.md)

---

## License

Koalesce is licensed under the [MIT License](https://github.com/falberthen/Koalesce/blob/master/LICENSE).