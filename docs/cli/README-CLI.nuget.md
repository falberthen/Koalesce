# Koalesce CLI for OpenAPI

![Koalesce](https://raw.githubusercontent.com/falberthen/Koalesce/master/img/koalesce_small.png)<img src="https://raw.githubusercontent.com/falberthen/Koalesce/master/img/cli_icon_256x256.png" heigth="170" width="170" />

The `Koalesce.OpenAPI.CLI` is a standalone tool that uses [Koalesce.OpenAPI](https://github.com/falberthen/Koalesce#readme) to merge OpenAPI definitions directly into a file in the disk.

> **Official packages are published exclusively to [NuGet.org](https://www.nuget.org/packages?q=Koalesce) by the maintainer.** Do not trust packages from unofficial sources.

---

## Quick Start

### 1. Install Koalesce CLI for OpenAPI

Install globally to merge OpenAPI specs without hosting an app:

```bash
dotnet tool install --global Koalesce.OpenAPI.CLI --prerelease
```

### 2. Use it!

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

## Configuration Examples

### Aggregation Mode

```json
{
  "Koalesce": {
    "Sources": [
      { "Url": "https://service1.com/swagger.json" },
      { "Url": "https://service2.com/swagger.json" }
    ],
    "MergedDocumentPath": "/swagger/v1/all-apis.json",
    "Title": "All APIs Documentation"
  }
}
```

### Gateway Mode (With Caching)

```json
{
  "Koalesce": {
    "Sources": [
      {
        "Url": "https://localhost:8001/swagger/v1/swagger.json",
        "VirtualPrefix": "/customers"
      },
      {
        "Url": "https://localhost:8002/swagger/v1/swagger.json",
        "VirtualPrefix": "/inventory"
      }
    ],
    "MergedDocumentPath": "/swagger/v1/apigateway.json",
    "Title": "API Gateway",
    "ApiGatewayBaseUrl": "https://localhost:5000", // <-----
    "Cache": {  // <-----
      "AbsoluteExpirationSeconds": 86400,
      "SlidingExpirationSeconds": 300
    }
  }
}
```

### Mixed Sources (HTTP + Local Files)

Useful when merging live APIs with downloaded specifications from public APIs:

```json
{
  "Koalesce": {
    "Sources": [
      { "Url": "https://localhost:8001/swagger/v1/swagger.json" },
      { "FilePath": "./specs/external-api.json" }
    ],
    "MergedDocumentPath": "/swagger/v1/merged.json",
    "Title": "Combined API Documentation"
  }
}
```

> **Note:** File paths can be absolute or relative. Relative paths are resolved from the application's base directory.

### Strict Mode

```json
{
  "Koalesce": {
    "Sources": [
      { "Url": "https://localhost:8001/swagger/v1/swagger.json" },
      { "Url": "https://localhost:8002/swagger/v1/swagger.json" }
    ],
    "MergedDocumentPath": "/swagger/v1/apigateway.yaml",
    "Title": "API Gateway",
    "ApiGatewayBaseUrl": "https://localhost:5000",
    "FailOnServiceLoadError": true, // <-----
    "SkipIdenticalPaths": false     // <-----
  }
}
```

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