# Koalesce CLI

![Koalesce](https://raw.githubusercontent.com/falberthen/Koalesce/master/img/cli_small.png)

The `Koalesce.CLI` is a standalone tool that uses [Koalesce](https://github.com/falberthen/Koalesce#readme) to merge OpenAPI definitions directly into a file on the disk.

---

## Quick Start

### 1. Install Koalesce CLI

Install globally to merge OpenAPI specs without hosting an app:

```bash
dotnet tool install --global Koalesce.CLI --prerelease
```

### 2. Use it!

```bash
koalesce --config ./appsettings.json --output ./gateway.yaml --verbose
```

![CLI](https://raw.githubusercontent.com/falberthen/Koalesce/master/img/Screenshot_CLI.png)

#### Arguments
- 🔺 `--config` - Path to your `appsettings.json`
- 🔺 `--output` - Path for the merged OpenAPI spec file
- `--verbose` - Enable detailed logging
- `--version` - Display current version

> 💡 **Note:** The CLI uses the same configuration model as the Middleware, except `Cache` and `MergedEndpoint`.

---

## 📝 Configuration Examples

### Minimal

```json
{
  "Koalesce": {
    "Sources": [
      { "Url": "https://service1.com/swagger.json" },
      { "Url": "https://service2.com/swagger.json" }
    ],
    "MergedEndpoint": "/swagger/v1/all-apis.json"
  }
}
```

### Advanced

```json
{
  "Koalesce": {
    "Title": "API Gateway",
    "OpenApiVersion": "3.1.0",
    "Sources": [
      {
        "Url": "https://localhost:8001/swagger/v1/swagger.json",
        "VirtualPrefix": "/customers"
      },
      {
        "Url": "https://localhost:8002/swagger/v1/swagger.json",
        "VirtualPrefix": "/inventory"
      },
      { "FilePath": "./specs/external-api.json" }
    ],
    "MergedEndpoint": "/swagger/v1/apigateway.json",
    "ApiGatewayBaseUrl": "https://localhost:5000",
    "HttpTimeoutSeconds": 30,
    "Cache": {
      "AbsoluteExpirationSeconds": 86400,
      "SlidingExpirationSeconds": 300
    }
  }
}
```

---

## 🔀 Conflict Resolution

### Path Conflicts

Use `VirtualPrefix` to preserve all endpoints:

```json
{
  "Sources": [
    { "Url": "https://inventory-api/swagger.json", "VirtualPrefix": "/inventory" },
    { "Url": "https://catalog-api/swagger.json", "VirtualPrefix": "/catalog" }
  ]
}
```

Or set `"SkipIdenticalPaths": false` to fail-fast on conflicts.

### Schema Conflicts

When multiple APIs define schemas with identical names, Koalesce automatically renames them using `{Prefix}{SchemaName}`.

---

## 📜 Documentation & Links

- [Full Documentation](https://github.com/falberthen/Koalesce#readme)
- [Sample Projects](https://github.com/falberthen/Koalesce/tree/master/samples)
- [Koalesce Changelog](https://github.com/falberthen/Koalesce/blob/master/docs/CHANGELOG.md)
- [Koalesce CLI Changelog](https://github.com/falberthen/Koalesce/tree/master/docs/cli/CHANGELOG.md)
- [Contributing](https://github.com/falberthen/Koalesce/blob/master/docs/CONTRIBUTING.md)

---

## 📧 Support

- **Issues**: [GitHub Issues](https://github.com/falberthen/Koalesce/issues)
- **Samples**: [Koalesce.Samples](https://github.com/falberthen/Koalesce/tree/master/samples)

---

## 📝 License

Koalesce is licensed under the [MIT License](https://github.com/falberthen/Koalesce/blob/master/LICENSE).
