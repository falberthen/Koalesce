# Koalesce

![Koalesce](https://raw.githubusercontent.com/falberthen/Koalesce/master/img/koalesce_small.png)

**Koalesce** is an open-source, lightweight and extensible library designed to merge multiple API definitions into a unified document.

⭐ **If you find Koalesce useful, please consider giving it a star!** It helps others discover the project.

![CI Status](https://github.com/falberthen/Koalesce/actions/workflows/tests.yml/badge.svg) ![GitHub Issues](https://img.shields.io/github/issues/falberthen/Koalesce) [![Donate](https://img.shields.io/badge/Donate-PayPal-blue.svg)](https://www.paypal.com/donate?business=CFZAMDPCTKZY6&item_name=Koalesce&currency_code=CAD)

---

## How It Works

- Koalesce fetches API definitions from the specified **Sources**.
- It merges them using an available provider (e.g., `Koalesce.OpenAPI`), generating a single schema at **MergedDocumentPath**.
- The final *Koalesced* API definition is serialized and available in `JSON` or `YAML` format.

### ⚡ Key Features

- ✅ **Merge Multiple APIs**: Coalesce multiple API definitions into one unified schema.
- ✅ **Conflict Resolution**: Automatic schema renaming and path collision detection.
- ✅ **Flexible Configuration**: Configure via `appsettings.json` or Fluent API.
- ✅ **Fail-Fast Validation**: Validates URLs and paths at startup to prevent runtime errors.
- ✅ **Gateway Integration**: Works seamlessly with **Ocelot**, **YARP**, and other API Gateways.
- ✅ **Configurable Caching**: Fine-grained cache control with absolute/sliding expiration settings.
- ✅ **Ease Client Generation**: Streamlines API client generation (e.g., **NSwag**, **Kiota**) with a single unified schema.
- ✅ **Format Agnostic Output**: Output `JSON` or `YAML` regardless of source document format.
- ✅ **Extensible Core**: Designed to support future providers for other API specification formats.

### 🧠 Design Philosophy

**Koalesce** balances **Developer Experience** with architectural governance:

* **Resilient by Default:** If a microservice is down, Koalesce skips it without breaking your Gateway.
* **Strict by Choice:** Can be configured to fail on unreachable services or route collisions - useful for CI/CD pipelines.
* **Purposefully Opinionated:** Ensures merged definitions have clean, deterministic, and conflict-free naming.

### 🌞 Where Koalesce Shines

**Koalesce** is ideal for **Backend-for-Frontend (BFF)** patterns where external consumers need a unified API view.

- **Frontend applications** consuming an API Gateway.
- **SDK generation** with tools like `NSwag`/`Kiota` from a single unified schema.
- **Third-party developer portals** exposing your APIs.
- **External API consumers** needing consolidated documentation.

> 💡 **Tip:** For internal service-to-service communication, prefer direct service calls with dedicated clients per service to avoid tight coupling and unnecessary Gateway overhead.

---

## 📦 Installation

#### 🟢 Koalesce for OpenAPI Middleware (ASP.NET Core)

![NuGet](https://img.shields.io/nuget/vpre/Koalesce.OpenAPI.svg) ![NuGet Downloads](https://img.shields.io/nuget/dt/Koalesce.OpenAPI.svg)

```sh
# Package Manager
Install-Package Koalesce.OpenAPI -IncludePrerelease
```
```sh
# .NET CLI
dotnet add package Koalesce.OpenAPI --prerelease
```

#### 🟢 Koalesce.OpenAPI.CLI as a Global Tool

![NuGet](https://img.shields.io/nuget/vpre/Koalesce.OpenAPI.CLI.svg) ![NuGet Downloads](https://img.shields.io/nuget/dt/Koalesce.OpenAPI.CLI.svg)

```bash
dotnet tool install --global Koalesce.OpenAPI.CLI --prerelease
```

<br/>

> ⚠️ **Official packages are published exclusively to [NuGet.org](https://www.nuget.org/packages?q=Koalesce) by the maintainer.** Do not trust packages from unofficial sources.

---

## ⚙️ Configuration

Koalesce configuration is divided into **Core Options** and **Provider Options** (e.g., OpenAPI).

- 💡 Parameters marked with 🔺 are required
- 💡 The file extension `[.json, .yaml]` in **MergedDocumentPath** determines the output format

### 1️⃣ Core Configuration (`Koalesce`)

| Setting | Type | Default | Description |
|---|---|---|---|
| `Sources` | `array` | 🔺 | List of API sources. Each item contains `Url`, optional `VirtualPrefix`, and optional `ExcludePaths` |
| `MergedDocumentPath` | `string` | 🔺 | Path where the merged API definition is exposed |
| `Title` | `string` | `"My 🐨Koalesced API"` | Title for the merged API definition |
| `SkipIdenticalPaths` | `boolean` | `true` | If `false`, throws exception on duplicate paths. If `true`, logs warning and skips duplicates |
| `SchemaConflictPattern` | `string` | `"{Prefix}_{SchemaName}"` | Pattern for resolving schema name conflicts. Available placeholders: `{Prefix}`, `{SchemaName}` |
| `FailOnServiceLoadError` | `boolean` | `false` | If `true`, aborts startup if ANY source is unreachable. If `false` (default), logs error and skips the source. |
| `HttpTimeoutSeconds` | `integer` | `15` | HTTP request timeout in seconds for fetching API specifications |

#### Source Configuration

Each source must have either `Url` **or** `FilePath`, but not both.

| Setting | Type | Default | Description |
|---|---|---|---|
| `Url` | `string` | — | URL of the API definition (must be absolute HTTP/HTTPS URL). Mutually exclusive with `FilePath`. |
| `FilePath` | `string` | — | Local file path to the API definition (JSON or YAML). Mutually exclusive with `Url`. |
| `VirtualPrefix` | `string` | `null` | Optional prefix to apply to routes (e.g., `/inventory`) |
| `ExcludePaths` | `array` | `null` | Optional list of paths to exclude from merge. Supports exact matches and wildcards (e.g., `"/api/admin/*"`) |

### Caching Configuration

| Setting | Type | Default | Description |
|---|---|---|---|
| `DisableCache` | `boolean` | `false` | If `true`, recomputes the merged document on every request |
| `AbsoluteExpirationSeconds` | `integer` | `86400` (24h) | Max duration before a forced refresh of merged result |
| `SlidingExpirationSeconds` | `integer` | `300` (5 min) | Resets expiration on every access |
| `MinExpirationSeconds` | `integer` | `30` | Minimum allowed expiration time |

<br/>

### 2️⃣ OpenAPI Provider Configuration

These settings are specific to the `Koalesce.OpenAPI` provider.

| Setting | Type | Default | Description |
|---|---|---|---|
| `OpenApiVersion` | `string` | `"3.0.1"` | Target OpenAPI version for the output |
| `ApiGatewayBaseUrl` | `string` | `null` | The public URL of your Gateway. Activates **Gateway Mode** |

---

## 🔌 How to Use

### ⚙️ As Middleware in ASP.NET Core

#### 1️⃣ Register Koalesce

```csharp
builder.Services.AddKoalesce(builder.Configuration)
    .ForOpenAPI();
```

#### 2️⃣ Enable Middleware

```csharp
app.UseKoalesce();
```

<br/>

### <img src="https://raw.githubusercontent.com/falberthen/Koalesce/master/img/cli_icon_256x256.png" heigth="32" width="32" />  As CLI (Command Line Interface) Tool

The `Koalesce.OpenAPI.CLI` is a standalone tool that uses `Koalesce.OpenAPI` to merge OpenAPI definitions directly into a file `without hosting a .NET application`.

<img src="https://raw.githubusercontent.com/falberthen/Koalesce/master/img/Screenshot_CLI.png"/>

#### Arguments:

- 🔺 `--config` - Path to your `appsettings.json`
- 🔺 `--output` - Path for the merged OpenAPI spec file
- `--verbose` - Enable detailed logging
- `--version` - Display current version

#### Example

```bash
koalesce --config ./config/appsettings.json --output ./merged-specs/apigateway.yaml --verbose
```

> 💡 **Note:** The CLI uses the same configuration model as the Middleware. All settings are defined in `appsettings.json`.

---

## 🔐 Security Schemas

Koalesce is **non-opinionated** about security - authentication and authorization are responsibilities of your APIs and Gateway.

**Default Behavior:**

- ✅ Operations with security in downstream APIs → Keep their security requirements
- ✅ Operations without security in downstream APIs → Remain public
- ✅ Mixed public/private scenarios are supported naturally
- ✅ Each API's security scheme is preserved in the merged document

---

## 🔀 Conflict Resolution Strategies

### Path Conflict Resolution

When multiple APIs define identical routes (e.g., `/api/health`), Koalesce handles conflicts based on your configuration. Choose the strategy that best fits your architecture:

**Scenario 1: Preserve All Endpoints wIth a `VirtualPrefix` (Recommended)**

Use when you want to preserve All endpoints from All APIs:

```json
{
  "Sources": [
    { "Url": "https://inventory-api/swagger.json", "VirtualPrefix": "/inventory" },
    { "Url": "https://catalog-api/swagger.json", "VirtualPrefix": "/catalog" }
  ]
}
```

**Behavior:**

- ✅ Transforms `/api/health` → `/inventory/api/health` and `/catalog/api/health`
- ✅ Both endpoints preserved in merged document
- ✅ No path conflicts occur
- ⚠️ **Requires Gateway URL Rewrite** to route prefixed paths back to original services

<br/>

**Scenario 2: First Source Wins (Default)**

Use when you have overlapping routes and want Koalesce to handle it automatically:

```json
{
  "Sources": [
    { "Url": "https://inventory-api/swagger.json" },
    { "Url": "https://catalog-api/swagger.json" }
  ],
}
```

**Behavior:**

- ✅ First source wins: `/api/health` from `inventory-api` is kept
- ⚠️ Subsequent identical paths are **skipped** with warning
- ⚠️ `/api/health` from `catalog-api` is **lost** in merged document
- ✅ No Gateway configuration needed

<br/>

**Scenario 3: Enforce Unique Routes, Fail-Fast on Conflicts**

Use when you want to enforce unique routes and fail if conflicts are detected:

```json
{
  "Sources": [
    { "Url": "https://inventory-api/swagger.json" },
    { "Url": "https://catalog-api/swagger.json" }
  ],
  "SkipIdenticalPaths": false
}
```

**Behavior:**

- ❌ **Throws `KoalesceIdenticalPathFoundException` at startup**
- ❌ Merge fails if any path collision detected
- ✅ Forces explicit conflict resolution

<br/>

### Schema Name Conflict Resolution

**Automatic Resolution:** When multiple APIs define schemas with identical names (e.g., `Product`), Koalesce automatically renames them using the pattern `{Prefix}_{SchemaName}`.

**Conflict Behavior:**

| Scenario | Result |
|---|---|
| Both sources have `VirtualPrefix` | **Both** schemas are renamed (e.g., `Inventory_Product`, `Catalog_Product`.) |
| Only one source have `VirtualPrefix` | Only the prefixed source's schema is renamed |
| Neither source has `VirtualPrefix` | First schema keeps original name. Second uses **Sanitized API Title** as prefix. |

> 💡 **Note:** When falling back to the API Title, Koalesce sanitizes the string (PascalCase, alphanumeric only) to ensure valid C# identifiers. For example, `"Sales API v2"` becomes `SalesApiV2`.

**Prefix Priority:**

1. **VirtualPrefix** (if configured): `/inventory` → `Inventory_Product`
2. **API Name** (sanitized): `Koalesce.Samples.InventoryAPI` → `KoalesceSamplesInventoryAPI_Product`

---

## 📝 Configuration Examples (Koalesce.OpenAPI)

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

> 💡 **Note:** File paths can be absolute or relative. Relative paths are resolved from the application's base directory.

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

> 💡 **Note:** Check out the [Koalesce.Samples](https://github.com/falberthen/Koalesce/tree/master/samples) projects for complete working examples.

---

## 📜 Changelog

- [Koalesce Changelog](https://github.com/falberthen/Koalesce/blob/master/docs/CHANGELOG.md)
- [Koalesce CLI Changelog](https://github.com/falberthen/Koalesce/tree/master/docs/cli/CHANGELOG.md)

---

## 📧 Support & Contributing

- **Issues**: Report bugs or request features via [GitHub Issues](https://github.com/falberthen/Koalesce/issues)
- **Contributing**: Contributions are welcome! Please read [CONTRIBUTING.md](https://github.com/falberthen/Koalesce/tree/master/docs/CONTRIBUTING.md) before submitting PRs.
- **Sample Projects**: Check out [Koalesce.Samples.sln](https://github.com/falberthen/Koalesce/blob/master/samples/Koalesce.Samples.sln) for a complete implementation

---

## 📝 License

Koalesce is licensed under the [**MIT License**](https://github.com/falberthen/Koalesce/blob/master/LICENSE).
