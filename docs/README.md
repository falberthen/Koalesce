# Koalesce

![Koalesce](https://raw.githubusercontent.com/falberthen/Koalesce/master/img/koalesce_small.png)

**Koalesce** is an open-source, lightweight library that merges multiple `OpenAPI` definitions into a single unified definition.

![.NET](https://img.shields.io/badge/.NET-8-512BD4?logo=dotnet) ![.NET](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet)
![CI Status](https://github.com/falberthen/Koalesce/actions/workflows/tests.yml/badge.svg) ![GitHub Issues](https://img.shields.io/github/issues/falberthen/Koalesce) [![Donate](https://img.shields.io/badge/Donate-PayPal-blue.svg)](https://www.paypal.com/donate?business=CFZAMDPCTKZY6&item_name=Koalesce&currency_code=CAD)

⭐ **If you find Koalesce useful, please consider giving it a star!** It helps others discover the project.

---

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

### 🧠 Design Philosophy

**Koalesce** balances **Developer Experience** with architectural governance:

- **Resilient by Default:** If a microservice is down, Koalesce skips it without breaking your Gateway.
- **Strict by Choice:** Can be configured to fail on unreachable services or route collisions - useful for CI/CD pipelines.
- **Purposefully Opinionated:** Ensures merged definitions have clean, deterministic, and conflict-free naming.

### 🌞 Where Koalesce Shines

**Koalesce** is ideal for **Backend-for-Frontend (BFF)** patterns where external consumers need a unified API view.

- **Frontend applications** consuming an API Gateway.
- **SDK generation** with tools like `NSwag`/`Kiota` from a single unified schema.
- **Third-party developer portals** exposing your APIs.
- **External API consumers** needing consolidated documentation.

> 💡 **Tip:** For internal service-to-service communication, prefer direct service calls with dedicated clients per service to avoid tight coupling and unnecessary Gateway overhead.

---

## 📦 Installation

### Koalesce (ASP.NET Core Middleware)

[![NuGet](https://img.shields.io/nuget/vpre/Koalesce.svg)](https://www.nuget.org/packages/Koalesce)

```sh
dotnet add package Koalesce --prerelease
```

### Koalesce.CLI (Global Tool)
[![NuGet](https://img.shields.io/nuget/vpre/Koalesce.CLI.svg)](https://www.nuget.org/packages/Koalesce.CLI)

```bash
dotnet tool install --global Koalesce.CLI --prerelease
```

<br/>

⚠️ **Migration:** The packages `Koalesce.OpenAPI v1.0.0-alpha.*` and `Koalesce.OpenAPI.CLI v1.0.0-alpha.*` are now deprecated.
 Please migrate to `Koalesce` and `Koalesce.CLI`.


---

## ⚙️ Configuration

💡 Parameters marked with 🔺 are required.

| Setting | Type | Default | Description |
| --- | --- | --- | --- |
| `Sources` | `array` | 🔺 | List of API sources to merge. |
| `Title` | `string` | `"Koalesced API"` | Title for the merged API definition. |
| `OpenApiVersion` | `string` | `"3.0.1"` | Target OpenAPI version for the output. |
| `ApiGatewayBaseUrl` | `string` | `null` | Public URL of your Gateway. Enables **Gateway Mode**. |
| `SkipIdenticalPaths` | `boolean` | `true` | If `false`, throws on duplicate paths. If `true`, logs warning and skips. |
| `SchemaConflictPattern` | `string` | `"{Prefix}{SchemaName}"` | Pattern for resolving schema name conflicts. |
| `FailOnServiceLoadError` | `boolean` | `false` | If `true`, aborts startup when any source is unreachable. |
| `HttpTimeoutSeconds` | `integer` | `15` | Timeout in seconds for fetching API specifications. |

Each source must have either `Url` **or** `FilePath`, but not both.

| Setting | Type | Default | Description |
| --- | --- | --- | --- |
| `Url` | `string` | — | URL of the API definition. Mutually exclusive with `FilePath`. |
| `FilePath` | `string` | — | Local file path to the API definition. Mutually exclusive with `Url`. |
| `VirtualPrefix` | `string` | `null` | Prefix to apply to all routes (e.g., `/inventory`). |
| `ExcludePaths` | `array` | `null` | Paths to exclude. Supports wildcards (e.g., `"/api/admin/*"`). |

### Middleware only

If using it as a Middleware, you must specify the merged endpoint.

| Setting | Type | Default | Description |
| --- | --- | --- | --- |
| `MergedEndpoint` | `string` | 🔺 | HTTP endpoint where the merged definition is exposed. |

> 💡 **Tip:** Point your OpenAPI UI tool (Swagger UI, Scalar, Redoc, etc.) to this endpoint.

#### Cache

| Setting | Type | Default | Description |
| --- | --- | --- | --- |
| `DisableCache` | `boolean` | `false` | Recomputes merged document on every request. |
| `AbsoluteExpirationSeconds` | `integer` | `86400` | Max cache duration (24h). |
| `SlidingExpirationSeconds` | `integer` | `300` | Resets expiration on access (5 min). |

---

## 📦 Quick Start

### 1️⃣ As Middleware (ASP.NET Core)

- In your `Program.cs`, register Koalesce services.

  ```csharp
  builder.Services.AddKoalesce();
  ```

- Then, enable it!

  ```csharp
  app.UseKoalesce();
  ```

### 2️⃣ Through the CLI (Command Line Interface) Tool

- The CLI merges OpenAPI definitions directly into a file on the disk without hosting an application.

  ```bash
  koalesce -c .\settings.json -o .\Output\gateway.yaml
  ```

  ![Koalesce CLI Screenshot](https://raw.githubusercontent.com/falberthen/Koalesce/master/img/Screenshot_CLI.png)

  #### Arguments

  | Option       | Shortcut   | Required | Description                                                   |
  | ------------ | ---------- | -------- | ------------------------------------------------------------- |
  | `--config`   | `-c`       | 🔺Yes   | Path to your `appsettings.json` (default: `appsettings.json`) |
  | `--output`   | `-o`       | 🔺Yes   | Path for the merged OpenAPI spec file                         |
  | `--insecure` | `-k`, `-i` | No       | Skip SSL certificate validation (for self-signed certs)       |
  | `--verbose`  |            | No       | Enable detailed logging                                       |
  | `--version`  |            | No       | Display current version                                       |

  > 💡 **Note:** The CLI uses the same configuration model as the Middleware, except `Cache` and `MergedEndpoint`.

---

## 🔀 Conflict Resolution

### Path Conflicts

When multiple APIs define identical routes (e.g., `/api/health`), Koalesce handles conflicts based on your configuration. Choose the strategy that best fits your architecture:

**Scenario 1: Preserve All Endpoints with a `VirtualPrefix` (Recommended)**

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

**Scenario 2: First Source Wins (Default)**

Use when you have overlapping routes and want Koalesce to handle it automatically:

```json
{
  "Sources": [
    { "Url": "https://inventory-api/swagger.json" },
    { "Url": "https://catalog-api/swagger.json" }
  ]
}
```

**Behavior:**

- ✅ First source wins: `/api/health` from `inventory-api` is kept
- ⚠️ Subsequent identical paths are **skipped** with warning
- ⚠️ `/api/health` from `catalog-api` is **lost** in merged document
- ✅ No Gateway configuration needed

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

### Schema Conflicts

**Automatic Resolution:** When multiple APIs define schemas with identical names (e.g., `Product`), Koalesce automatically renames them using the pattern `{Prefix}{SchemaName}`.

**Conflict Behavior:**

| Scenario | Result |
|---|---|
| Both sources have `VirtualPrefix` | **Both** schemas are renamed (e.g., `Inventory_Product`, `Catalog_Product`.) |
| Only one source has `VirtualPrefix` | Only the prefixed source's schema is renamed |
| Neither source has `VirtualPrefix` | First schema keeps original name. Second uses **Sanitized API Title** as prefix. |

> 💡 **Note:** When falling back to the API Title, Koalesce sanitizes the string (PascalCase, alphanumeric only) to ensure valid C# identifiers. For example, `"Sales API v2"` becomes `SalesApiV2`.

**Prefix Priority:**

1. **VirtualPrefix** (if configured): `/inventory` → `Inventory_Product`
2. **API Name** (sanitized): `Koalesce.Samples.InventoryAPI` → `KoalesceSamplesInventoryAPI_Product`

---

### HttpClient Customization

By default, Koalesce uses its own `HttpClient` with basic settings (timeout from `HttpTimeoutSeconds`, automatic decompression). For advanced scenarios (custom SSL/TLS, authentication handlers, retry policies), you can customize it:

```csharp
builder.Services.AddKoalesce(
    configuration,
    configureHttpClient: builder =>
    {
        // Example: Allow self-signed certificates (development only!)
        builder.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (msg, cert, chain, errors) => true
        });

        // Example: Add retry policy with Polly
        builder.AddPolicyHandler(GetRetryPolicy());

        // Example: Override timeout (ignores HttpTimeoutSeconds from config)
        builder.ConfigureHttpClient(client => client.Timeout = TimeSpan.FromSeconds(60));
    });
```

> 💡 **Note:** When using `configureHttpClient`, the `HttpTimeoutSeconds` setting is still applied as the default. Use `ConfigureHttpClient` inside the callback to override it if needed.

---
## 📝 Configuration Examples

### Minimal configuration

```json
{
  "Koalesce": {
    "Sources": [
      { "Url": "https://service1.com/swagger.json" },
      { "Url": "https://service2.com/swagger.json" }
    ],
    "MergedEndpoint": "/swagger/v1/all-apis.json",
  }
}
```

### Advanced configuration

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
    "SchemaConflictPattern": "{Prefix}_{SchemaName}",
    "Cache": {
      "AbsoluteExpirationSeconds": 86400,
      "SlidingExpirationSeconds": 300
    }
  }
}
```

### Strict configuration

```json
{
  "Koalesce": {
    "Title": "API Gateway",
    "Sources": [
      { "Url": "https://localhost:8001/swagger/v1/swagger.json" },
      { "Url": "https://localhost:8002/swagger/v1/swagger.json" }
    ],
    "MergedEndpoint": "/swagger/v1/apigateway.yaml",
    "ApiGatewayBaseUrl": "https://localhost:5000",
    "FailOnServiceLoadError": true, // <-----
    "SkipIdenticalPaths": false     // <-----
  }
}
```

> 💡 **Note:** Check out the [Koalesce.Samples](https://github.com/falberthen/Koalesce/tree/master/samples) projects for complete working examples.

---

## 📜 Changelog

- [Koalesce.Changelog](https://github.com/falberthen/Koalesce/blob/master/docs/CHANGELOG.md)
- [Koalesce.CLI Changelog](https://github.com/falberthen/Koalesce/tree/master/docs/cli/CHANGELOG.md)

---

## 📧 Support & Contributing

- **Issues**: Report bugs or request features via [GitHub Issues](https://github.com/falberthen/Koalesce/issues).
- **Contributing**: Contributions are welcome! Please read [CONTRIBUTING.md](https://github.com/falberthen/Koalesce/tree/master/docs/CONTRIBUTING.md) before submitting PRs.
- **Sample Projects**: Check out [Koalesce.Samples](https://github.com/falberthen/Koalesce/tree/master/samples) for a complete implementation.

---

## 📝 License

Koalesce is licensed under the [**MIT License**](https://github.com/falberthen/Koalesce/blob/master/LICENSE).

<br/>

<p align="center">
  Made with ❤️ by <a href="https://github.com/falberthen">Felipe Henrique</a>
</p>