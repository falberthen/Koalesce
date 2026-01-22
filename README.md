![CI Status](https://github.com/falberthen/Koalesce/actions/workflows/tests.yml/badge.svg)

<img src="img/koalesce_small.png" />

**Koalesce** is a .NET library designed to merge multiple API definitions into a unified document. It enables seamless API Gateway integration and simplifies frontend client generation for microservices-based architectures.

---

## How It Works

**Process:**
- Koalesce fetches API definitions from the specified **Sources**.
- It merges them using an available provider (e.g., `Koalesce.OpenAPI`), generating a single schema at **MergedDocumentPath**.
- The final *Koalesced* API definition is serialized and available in `JSON` or `YAML` format.

### ⚡ Key Features

- ✅ **Merge Multiple APIs**: Coalesce multiple API definitions into one unified schema.
- ✅ **Flexible Security**: Apply global Gateway security OR preserve downstream API security configurations.
- ✅ **Conflict Resolution**: Automatic schema renaming and path collision detection.
- ✅ **Configurable Caching**: Fine-grained cache control with absolute/sliding expiration settings.
- ✅ **Gateway Integration**: Works seamlessly with **Ocelot**, **YARP**, and other API Gateways.
- ✅ **Client Generation**: Streamlines API client generation (e.g., **NSwag**, **Kiota**) with a single unified schema.
- ✅ **Flexible Configuration**: Configure via `appsettings.json` or Fluent API.
- ✅ **Format Agnostic Output**: Output `JSON` or `YAML` regardless of source document format.
- ✅ **Fail-Fast Validation**: Validates URLs and paths at startup to prevent runtime errors.
- ✅ **Multi-targeting**: Native support for **.NET 8.0 (LTS)** and **.NET 10.0**.
- ✅ **Extensible Core**: Designed to support future providers for other API specification formats.

---

## 📦 Installation

#### 🟢 Koalesce for OpenAPI Middleware (ASP.NET Core)

![NuGet](https://img.shields.io/nuget/vpre/Koalesce.OpenAPI.svg)

```sh
# Package Manager
Install-Package Koalesce.OpenAPI -IncludePrerelease
```
```sh
# .NET CLI
dotnet add package Koalesce.OpenAPI --prerelease
```

#### 🟢 Koalesce.OpenAPI.CLI as a Global Tool

![NuGet](https://img.shields.io/nuget/vpre/Koalesce.OpenAPI.CLI.svg)

```bash
dotnet tool install --global Koalesce.OpenAPI.CLI --prerelease
```

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

#### Source Configuration

| Setting | Type | Default | Description |
|---|---|---|---|
| `Url` | `string` | 🔺 | URL of the API definition (must be absolute URL) |
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
| `OpenApiSecurityScheme` | `object` | `null` | **Optional** global security scheme. When configured, replaces all downstream security. When omitted, preserves downstream security as-is |

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

### 💻 As CLI (Command Line Interface) Tool

The `Koalesce.OpenAPI.CLI` is a standalone tool that uses `Koalesce.OpenAPI` to merge OpenAPI definitions directly into a file `without hosting a .NET application`.

<img src="img/Screenshot_CLI.png"/>

#### Arguments:

- 🔺 `--config` - Path to your `appsettings.json`
- 🔺 `--output` - Path for the merged OpenAPI spec file
- `--verbose` - Enable detailed logging
- `--version` - Display current version

#### Example

```bash
koalesce --config ./config/appsettings.json --output ./merged-specs/apigateway.yaml --verbose
```

> 💡 **Note:** The CLI uses the same configuration model as the Middleware. All settings are defined in `appsettings.json`, including optional security configuration.

---

## 🔐 Security Configuration (Optional)

Koalesce is **non-opinionated** about security - authentication and authorization are responsibilities of your APIs and Gateway.

**Default Behavior:**

- ✅ Operations with security in downstream APIs → Keep their security requirements
- ✅ Operations without security in downstream APIs → Remain public
- ✅ Mixed public/private scenarios are supported naturally
- ✅ Each API's security scheme is preserved in the merged document

However, it provides an **optional global security scheme** to simplify client generation and avoid post-processing the merged definition.

**Configuration:**

```json
{
  "Koalesce": {
    "ApiGatewayBaseUrl": "https://gateway.com",
    "OpenApiSecurityScheme": {
      "Type": "Http",
      "Scheme": "bearer",
      "BearerFormat": "JWT",
      "Description": "JWT Authorization"
    }
  }
}
```

**Result:**

- ✅ All operations in the merged document require Gateway authentication
- ✅ Downstream security schemes are **replaced** with the global scheme
- ✅ Ideal for NSwag/Kiota client generation with centralized auth
- ✅ All APIs become secured (even if they were public downstream)

#### (Optional) Configure Security via Fluent API

Koalesce provides fluent extension methods for common security scenarios:

**Available Extension Methods:**

- `ApplyGlobalJwtBearerSecurityScheme` - JWT Bearer Token authentication
- `ApplyGlobalApiKeySecurityScheme` - API Key authentication (Header, Query, or Cookie)
- `ApplyGlobalBasicAuthSecurityScheme` - HTTP Basic Authentication
- `ApplyGlobalOAuth2ClientCredentialsSecurityScheme` - OAuth2 Client Credentials flow
- `ApplyGlobalOAuth2AuthCodeSecurityScheme` - OAuth2 Authorization Code flow
- `ApplyGlobalOpenIdConnectSecurityScheme` - OpenID Connect (OIDC) Discovery

> 💡 **Tip:** If using the Middleware, specify security configuration via Fluent API to keep your `appsettings.json` clean.


```csharp
builder.Services.AddKoalesce(builder.Configuration)
    .ForOpenAPI(options =>
    {
        // Example 1: JWT Bearer (Most Common)
        options.ApplyGlobalJwtBearerSecurityScheme(
            schemeName: "Bearer",
            description: "Enter your JWT token"
        );

        /* Other examples:

        // Example 2: API Key
        options.ApplyGlobalApiKeySecurityScheme(
            headerName: "X-Api-Key",
            description: "Enter your API Key",
            location: ParameterLocation.Header
        );

        // Example 3: OAuth2 Client Credentials
        options.ApplyGlobalOAuth2ClientCredentialsSecurityScheme(
            tokenUrl: new Uri("https://auth.example.com/connect/token"),
            scopes: new Dictionary<string, string>
            {
                { "api.read", "Read Access" },
                { "api.write", "Write Access" }
            }
        );

        // Example 4: OpenID Connect (OIDC)
        options.ApplyGlobalOpenIdConnectSecurityScheme(
            openIdConnectUrl: new Uri("https://auth.example.com/.well-known/openid-configuration")
        );
        */
    });
```

---

## 🔀 Conflict Resolution Strategies

Koalesce automatically handles conflicts during the merge process:

### Schema Name Conflicts

When multiple APIs define schemas with the same name (e.g., `Product`), Koalesce automatically renames them using a configurable pattern.

**Default Pattern:** `{Prefix}_{SchemaName}`

**Example:**

- `InventoryAPI` defines `Product` → becomes `Inventory_Product`
- `CatalogAPI` defines `Product` → becomes `Catalog_Product`

**Custom Pattern:**

You can customize the pattern via `appsettings.json` or Fluent API:

```json
{
  "Koalesce": {
    "SchemaConflictPattern": "{SchemaName}_{Prefix}"
  }
}
```

**Available placeholders:** `{Prefix}`, `{SchemaName}`

**Prefix Priority:**

1. **VirtualPrefix** (if configured, e.g., `/inventory` → `Inventory`)
2. **API Name** (sanitized, e.g., `Koalesce.Samples.InventoryAPI` → `KoalesceSamplesInventoryAPI`)

This ensures all schemas are preserved without collisions.

### Path Conflicts

When identical paths exist across multiple APIs (e.g., `/api/health`), you have two options:

**Option 1: Use `VirtualPrefix` (Recommended)**

```json
{
  "Sources": [
    {
      "Url": "https://inventory-api/swagger.json",
      "VirtualPrefix": "/inventory"
    },
    {
      "Url": "https://catalog-api/swagger.json",
      "VirtualPrefix": "/catalog"
    }
  ]
}
```

**Result:** `/api/health` becomes `/inventory/api/health` and `/catalog/api/health`

> **Important:** Your API Gateway (Ocelot/YARP) must be configured to route these prefixed paths back to the original downstream services.

<br/>

**Option 2: Set `SkipIdenticalPaths: true`**

```json
{
  "SkipIdenticalPaths": true
}
```

**Result:** First API wins, subsequent identical paths are skipped with a warning.

### Excluding Specific Paths

You can exclude specific paths from being merged using the `ExcludePaths` option per source. This is useful for:

- Hiding internal/admin endpoints from the public API documentation
- Preventing path conflicts without using VirtualPrefix
- Excluding deprecated or experimental endpoints

**Example:**

```json
{
  "Sources": [
    {
      "Url": "https://api.example.com/swagger.json",
      "ExcludePaths": [
        "/api/internal",
        "/api/admin/*"
      ]
    }
  ]
}
```

**Supported Patterns:**

- **Exact match:** `"/api/internal"` - excludes only that exact path
- **Wildcard:** `"/api/admin/*"` - excludes `/api/admin` and any path starting with `/api/admin/`

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
    // If OpenApiSecurityScheme = downstream security is preserved
  }
}
```

### Gateway Mode

```json
{
  "Koalesce": {
    "Sources": [
      { "Url": "https://localhost:8001/swagger/v1/swagger.json" },
      { "Url": "https://localhost:8002/swagger/v1/swagger.json" }
    ],
    "MergedDocumentPath": "/swagger/v1/apigateway.yaml",
    "Title": "API Gateway",
    "ApiGatewayBaseUrl": "https://localhost:5000"
    // If OpenApiSecurityScheme = downstream security is preserved
  }
}
```

### Gateway Mode (With Global Security and Cache)

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
    "OpenApiSecurityScheme": {
      "Type": "Http",
      "Scheme": "bearer",
      "BearerFormat": "JWT",
      "Description": "JWT Authorization"
    },
    "Cache": {
      "AbsoluteExpirationSeconds": 86400,
      "SlidingExpirationSeconds": 300
    }
  }
}
```

> 💡 **Note:** Check out the [Koalesce.Samples.Swagger.Ocelot](https://github.com/falberthen/Koalesce/tree/master/samples/Koalesce.Samples.Swagger.Ocelot) sample project for a complete working implementation with Ocelot Gateway integration.

---

## ⚠️ Important Considerations and Limitations

### Path Conflict Resolution

When multiple APIs define identical routes (e.g., `/api/health`), Koalesce handles conflicts based on your configuration. Choose the strategy that best fits your architecture:

**Scenario 1: With `VirtualPrefix` (Recommended) - Preserve All Endpoints**

Use when you want to preserve ALL endpoints from ALL APIs:

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

**Scenario 2: Without `VirtualPrefix` (Default) - First Source Wins**

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

**Scenario 3: Fail-Fast on Conflicts - Enforce Unique Routes**

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

### Schema Name Conflict Resolution

**Automatic Resolution:** When multiple APIs define schemas with identical names (e.g., `Product`), Koalesce automatically renames them using the pattern `{Prefix}_{SchemaName}`.

**Conflict Behavior:**

| Scenario | Result |
|---|---|
| Both sources have `VirtualPrefix` | **Both** schemas are renamed (e.g., `Inventory_Product`, `Catalog_Product`) |
| Only one source have `VirtualPrefix` | Only the prefixed source's schema is renamed |
| Neither source has `VirtualPrefix` | First schema keeps original name, second uses API name prefix |

**Prefix Priority:**

1. **VirtualPrefix** (if configured): `/inventory` → `Inventory_Product`
2. **API Name** (sanitized): `Koalesce.Samples.InventoryAPI` → `KoalesceSamplesInventoryAPI_Product`

**Example (both with VirtualPrefix):**

- `InventoryAPI` (`/inventory`) defines `Product` → becomes `Inventory_Product`
- `CatalogAPI` (`/catalog`) defines `Product` → becomes `Catalog_Product`

This ensures all schemas are preserved without manual intervention and naming is deterministic regardless of source order.

---

## 📜 Changelog

- [Koalesce Changelog](https://github.com/falberthen/Koalesce/blob/master/CHANGELOG.md)
- [Koalesce CLI Changelog](https://github.com/falberthen/Koalesce/tree/master/src/Koalesce.OpenAPI.CLI/CHANGELOG.md)

---

## 📧 Support & Contributing

- **Issues**: Report bugs or request features via [GitHub Issues](https://github.com/falberthen/Koalesce/issues)
- **Contributing**: Contributions are welcome! Feel free to open issues and submit PRs.
- **Sample Projects**: Check out [Koalesce.Samples.sln](https://github.com/falberthen/Koalesce/blob/master/samples/Koalesce.Samples.sln) for a complete implementation

---

## 📝 License

Koalesce is licensed under the [**MIT License**](https://github.com/falberthen/Koalesce/blob/master/LICENSE).
