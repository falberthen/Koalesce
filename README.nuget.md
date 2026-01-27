# Koalesce

![Koalesce](https://raw.githubusercontent.com/falberthen/Koalesce/master/img/koalesce_small.png)

**Koalesce** is an open-source, lightweight and extensible library designed to merge multiple API definitions into a unified document.

> **Official packages are published exclusively to [NuGet.org](https://www.nuget.org/packages?q=Koalesce) by the maintainer.** Do not trust packages from unofficial sources.

---

## How It Works

- Koalesce fetches API definitions from the specified **Sources**.
- It merges them using an available provider (e.g., `Koalesce.OpenAPI`), generating a single schema at **MergedDocumentPath**.
- The final *Koalesced* API definition is serialized and available in `JSON` or `YAML` format.

### Key Features

- **Merge Multiple APIs**: Coalesce multiple API definitions into one unified schema.
- **Conflict Resolution**: Automatic schema renaming and path collision detection.
- **Flexible Configuration**: Configure via `appsettings.json` or Fluent API.
- **Fail-Fast Validation**: Validates URLs and paths at startup to prevent runtime errors.
- **Gateway Integration**: Works seamlessly with **Ocelot**, **YARP**, and other API Gateways.
- **Configurable Caching**: Fine-grained cache control with absolute/sliding expiration settings.
- **Ease Client Generation**: Streamlines API client generation (e.g., **NSwag**, **Kiota**) with a single unified schema.
- **Format Agnostic Output**: Output `JSON` or `YAML` regardless of source document format.
- **Extensible Core**: Designed to support future providers for other API specification formats.

### Design Philosophy

**Koalesce** balances **Developer Experience** with architectural governance:

* **Resilient by Default:** If a microservice is down, Koalesce skips it without breaking your Gateway.
* **Strict by Choice:** Can be configured to fail on unreachable services or route collisions - useful for CI/CD pipelines.
* **Purposefully Opinionated:** Ensures merged definitions have clean, deterministic, and conflict-free naming.

### Where Koalesce Shines

**Koalesce** is ideal for **Backend-for-Frontend (BFF)** patterns where external consumers need a unified API view.

- **Frontend applications** consuming an API Gateway.
- **SDK generation** with tools like `NSwag`/`Kiota` from a single unified schema.
- **Third-party developer portals** exposing your APIs.
- **External API consumers** needing consolidated documentation.

---

## Quick Start

### 1. Register Koalesce

```csharp
builder.Services.AddKoalesce(builder.Configuration)
    .ForOpenAPI();
```

### 2. Enable Middleware

```csharp
app.UseKoalesce();
```

---

## Configuration

### Core Configuration (`Koalesce`)

| Setting | Type | Default | Description |
|---|---|---|---|
| `Sources` | `array` | *required* | List of API sources with `Url`, optional `VirtualPrefix`, and optional `ExcludePaths` |
| `MergedDocumentPath` | `string` | *required* | Path where the merged API definition is exposed |
| `Title` | `string` | `"My Koalesced API"` | Title for the merged API definition |
| `SkipIdenticalPaths` | `boolean` | `true` | If `false`, throws exception on duplicate paths |
| `SchemaConflictPattern` | `string` | `"{Prefix}_{SchemaName}"` | Pattern for resolving schema name conflicts |
| `FailOnServiceLoadError` | `boolean` | `false` | If `true`, aborts startup if ANY source is unreachable |

### Source Configuration

| Setting | Type | Default | Description |
|---|---|---|---|
| `Url` | `string` | *required* | URL of the API definition (must be absolute URL) |
| `VirtualPrefix` | `string` | `null` | Optional prefix to apply to routes (e.g., `/inventory`) |
| `ExcludePaths` | `array` | `null` | Optional list of paths to exclude. Supports wildcards (e.g., `"/api/admin/*"`) |

### Caching Configuration

| Setting | Type | Default | Description |
|---|---|---|---|
| `DisableCache` | `boolean` | `false` | If `true`, recomputes the merged document on every request |
| `AbsoluteExpirationSeconds` | `integer` | `86400` (24h) | Max duration before a forced refresh |
| `SlidingExpirationSeconds` | `integer` | `300` (5 min) | Resets expiration on every access |
| `MinExpirationSeconds` | `integer` | `30` | Minimum allowed expiration time |

### OpenAPI Provider Configuration

| Setting | Type | Default | Description |
|---|---|---|---|
| `OpenApiVersion` | `string` | `"3.0.1"` | Target OpenAPI version for the output |
| `ApiGatewayBaseUrl` | `string` | `null` | The public URL of your Gateway. Activates **Gateway Mode** |

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
    "ApiGatewayBaseUrl": "https://localhost:5000",
    "Cache": {
      "AbsoluteExpirationSeconds": 86400,
      "SlidingExpirationSeconds": 300
    }
  }
}
```

### Strict Mode (CI/CD)

```json
{
  "Koalesce": {
    "Sources": [
      { "Url": "https://localhost:8001/swagger/v1/swagger.json" },
      { "Url": "https://localhost:8002/swagger/v1/swagger.json" }
    ],
    "MergedDocumentPath": "/swagger/v1/apigateway.yaml",
    "FailOnServiceLoadError": true,
    "SkipIdenticalPaths": false
  }
}
```

---

## Security Schemas

Koalesce is **non-opinionated** about security - authentication and authorization are responsibilities of your APIs and Gateway.

- Operations with security in downstream APIs keep their security requirements
- Operations without security remain public
- Mixed public/private scenarios are supported naturally
- Each API's security scheme is preserved in the merged document

---

## Conflict Resolution

### Path Conflicts

| Strategy | Configuration | Behavior |
|---|---|---|
| **Preserve All** (Recommended) | Use `VirtualPrefix` | `/api/health` becomes `/inventory/api/health` |
| **First Wins** (Default) | No prefix, `SkipIdenticalPaths: true` | First source keeps path, duplicates skipped |
| **Fail-Fast** | `SkipIdenticalPaths: false` | Throws exception on any collision |

### Schema Name Conflicts

| Scenario | Result |
|---|---|
| Both sources have `VirtualPrefix` | Both schemas renamed (e.g., `Inventory_Product`, `Catalog_Product`) |
| Only one source has `VirtualPrefix` | Only prefixed source's schema is renamed |
| Neither has `VirtualPrefix` | First keeps original name, second uses sanitized API Title |

---

## CLI Tool

Install globally to merge OpenAPI specs without hosting an app:

```bash
dotnet tool install --global Koalesce.OpenAPI.CLI --prerelease
koalesce --config ./appsettings.json --output ./gateway.yaml --verbose
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
