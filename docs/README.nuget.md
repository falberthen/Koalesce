# Koalesce

![Koalesce](https://raw.githubusercontent.com/falberthen/Koalesce/master/img/koalesce_small.png)

**Koalesce** is an open-source, lightweight library that merges multiple `OpenAPI` definitions into a single unified definition.

![.NET](https://img.shields.io/badge/.NET-8-512BD4?logo=dotnet) ![.NET](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet)
![CI Status](https://github.com/falberthen/Koalesce/actions/workflows/tests.yml/badge.svg)

⭐ **If you find Koalesce useful, please consider giving it a star!** It helps others discover the project.

## How It Works

- Koalesce fetches `OpenAPI` definitions from the specified **Sources**.
- It merges them into a single definition exposed at **MergedEndpoint**.
- The final *Koalesced* API definition is serialized and available in `JSON` or `YAML` format.

### ⚡ Key Features

- ✅ **Merge Multiple APIs**: Coalesce multiple OpenAPI definitions into a unified one.
- ✅ **Conflict Resolution**: Automatic schema renaming and path collision detection.
- ✅ **Flexible Configuration**: Configure via `appsettings.json` or Fluent API (Middleware).
- ✅ **Fail-Fast Validation**: Validates URLs and paths at startup to prevent runtime errors.
- ✅ **Gateway Integration**: Works seamlessly with **Ocelot**, **YARP**, and other API Gateways.
- ✅ **Configurable Caching**: Fine-grained cache control with absolute/sliding expiration settings.
- ✅ **Ease Client Generation**: Streamlines API client generation (e.g., **NSwag**, **Kiota**) from a single definition.
- ✅ **Format Agnostic Output**: Output `JSON` or `YAML` regardless of source document format.

---

## 📦 Installation

```sh
dotnet add package Koalesce --prerelease
```

## Quick Start (ASP.NET Core Middleware)

In your `Program.cs`, register Koalesce services:

```csharp
builder.Services.AddKoalesce();
```

Then, enable it:

```csharp
app.UseKoalesce();
```

---

## ⚙️ Configuration

💡 Parameters marked with 🔺 are required.

### Global Settings

| Setting | Type | Default | Description |
| --- | --- | --- | --- |
| `Sources` | `array` | 🔺 | List of API sources to merge. |
| `MergedEndpoint` | `string` | 🔺 | HTTP endpoint where the merged definition is exposed. |
| `Title` | `string` | `"Koalesced API"` | Title for the merged API definition. |
| `OpenApiVersion` | `string` | `"3.0.1"` | Target OpenAPI version for the output. |
| `ApiGatewayBaseUrl` | `string` | `null` | Public URL of your Gateway. Enables **Gateway Mode**. |
| `SkipIdenticalPaths` | `boolean` | `true` | If `false`, throws on duplicate paths. If `true`, logs warning and skips. |
| `SchemaConflictPattern` | `string` | `"{Prefix}{SchemaName}"` | Pattern for resolving schema name conflicts. |
| `FailOnServiceLoadError` | `boolean` | `false` | If `true`, aborts startup when any source is unreachable. |
| `HttpTimeoutSeconds` | `integer` | `15` | Timeout in seconds for fetching API specifications. |

### Source Settings

Each source must have either `Url` **or** `FilePath`, but not both.

| Setting | Type | Default | Description |
| --- | --- | --- | --- |
| `Url` | `string` | — | URL of the API definition. Mutually exclusive with `FilePath`. |
| `FilePath` | `string` | — | Local file path to the API definition. Mutually exclusive with `Url`. |
| `VirtualPrefix` | `string` | `null` | Prefix to apply to all routes (e.g., `/inventory`). |
| `ExcludePaths` | `array` | `null` | Paths to exclude. Supports wildcards (e.g., `"/api/admin/*"`). |

### Cache Configuration

| Setting | Type | Default | Description |
| --- | --- | --- | --- |
| `DisableCache` | `boolean` | `false` | Recomputes merged document on every request. |
| `AbsoluteExpirationSeconds` | `integer` | `86400` | Max cache duration (24h). |
| `SlidingExpirationSeconds` | `integer` | `300` | Resets expiration on access (5 min). |

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
