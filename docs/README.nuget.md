# Koalesce

![Koalesce](https://raw.githubusercontent.com/falberthen/Koalesce/master/img/koalesce_small.png)

**Koalesce** is an open-source .NET library that merges multiple OpenAPI specifications into a single unified definition.

![.NET](https://img.shields.io/badge/.NET-8-512BD4?logo=dotnet) ![.NET](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet) [![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

⭐ **If you find Koalesce useful, please consider giving it a star on [GitHub](https://github.com/falberthen/Koalesce)!**

---

## 🧩 The Problem

Building microservices or modular APIs? You're probably dealing with:

- 🔀 Frontend teams juggling **multiple Swagger UIs** across services.
- 📚 Scattered API documentation with no **unified view for consumers**.
- 🔍 No single place to explore, test, or share your full API surface.
- 🛠️ Client SDK generation from **scattered, disconnected specs**.

---

## 💡The Solution

![Koalesce](https://raw.githubusercontent.com/falberthen/Koalesce/master/img/koalesce_diagram.png)

---

## 📐 How It Works

**1. Fetch APIs** 
- Read from URLs (`https://api.com/swagger.json`) or local files (`./path/localspec.yaml`). 
- Supports OpenAPI 2.0, 3.0.x, 3.1.x, 3.2.x in JSON and YAML formats.

**2. Resolve Conflicts** 
- Path conflicts are handled by your choice: *VirtualPrefix*, *First Wins*, or *Fail-Fast*. 
- Schema name collisions are auto-renamed based on configuration (e.g., `Inventory.Product` → `InventoryProduct`).

**3. Output**  
- A single unified OpenAPI spec (JSON or YAML), targeting any version, ready for Swagger UI, Scalar, Kiota, or NSwag.

---

### 🌞 Where Koalesce Shines

- ✅ **Backend-for-Frontend (BFF)**: Unify multiple microservices into one API contract for your frontend team.
- ✅ **Developer Portals**: Publish a single API reference for partners without exposing internal service boundaries.
- ✅ **Client SDK Generation**: Generate one SDK from the unified spec (Kiota, NSwag, AutoRest) instead of managing multiple clients.
- ✅ **CI/CD Validation**: Validate API contracts across all services in one step using strict mode.
- ✅ **Mixed OpenAPI Versions**: Merge specs from different OpenAPI versions (2.0, 3.0.x, 3.1.x) into one normalized output.

> 💡 **Tip:** For internal service-to-service communication, prefer direct service calls with dedicated clients per service to avoid tight coupling and unnecessary Gateway overhead.

---

### 🧠 Design Philosophy

**Koalesce** balances **Developer Experience** with architectural governance:

- **Resilient by Default:** Skips unreachable services and duplicate paths with warnings.
- **Strict by Choice:** Can be configured to fail on unreachable services or route collisions - useful for CI/CD pipelines or while developing.
- **Purposefully Opinionated:** Ensures merged definitions have clean, deterministic, and conflict-free naming.
- **DX First:** Designed to be easy to set up and use, with sensible defaults and clear error messages.

---

## 📦 Quick Start

### 1️⃣ Install

Install it based on how you want to use Koalesce.

```sh
# Koalesce as an ASP.NET Core Middleware (for applications)
dotnet add package Koalesce --prerelease
```

```bash
# Koalesce as a CLI standalone tool
dotnet tool install --global Koalesce.CLI --prerelease
```

### 2️⃣ Configure

```json
// appsettings.json
{
  "Koalesce": {
    "OpenApiVersion": "3.0.1",
    "Title": "My Koalesced API",
    "Sources": [      
      {
        "Url": "https://localhost:8002/swagger/v1/swagger.json",
        "VirtualPrefix": "/catalog",
        "ExcludePaths": ["/internal/*", "*/admin/*"]
      },
      {
        "Url": "https://localhost:8003/swagger/v1/swagger.json",
        "VirtualPrefix": "/inventory"
      }
    ],    
    "MergedEndpoint": "/swagger/v1/apigateway.yaml" // ignored when using CLI
  }
}
```

### 3️⃣ Run it

#### Option A: Middleware (ASP.NET Core)
```csharp
// Program.cs
builder.Services.AddKoalesce();
app.UseKoalesce();

app.UseSwaggerUI(c =>
{
  c.SwaggerEndpoint(koalesceOptions.MergedEndpoint, koalesceOptions.Title);
});
```

![Koalesce CLI Screenshot](https://raw.githubusercontent.com/falberthen/Koalesce/master/img/Screenshot_Swagger.png)

#### Option B: Using the CLI Tool
```bash
  koalesce -c .\appsettings.json -o .\Output\apigateway.yaml
```

##### CLI arguments

| Option       | Shortcut   | Required |                                                  |
| ------------ | ---------- | -------- | ----------------------------------------------------------- |
| `--config`   | `-c`       | 🔺Yes   | Path to your configuration `.json` file.                    |
| `--output`   | `-o`       | 🔺Yes   | Path for the merged OpenAPI spec file.                      |
| `--insecure` | `-k`, `-i` | No       | Skip SSL certificate validation (for self-signed certs).    |
| `--verbose`  |            | No       | Enable detailed logging.                                    |
| `--version`  |            | No       | Display current version.                                    |

![Koalesce CLI Screenshot](https://raw.githubusercontent.com/falberthen/Koalesce/master/img/Screenshot_CLI_Sample.png)

💡 The CLI merges OpenAPI definitions directly into a file on disk without requiring a host application.

---

## ⚙️ Configuration Reference

#### Required Settings

| Setting | Type | Required |   |
|---------|---------|-------------|---|
| `Sources` | `array` | 🔺(*Middleware / CLI*) | List of API sources (see below) |
| `MergedEndpoint` | `string` | 🔺(*Middleware*) | HTTP endpoint for merged spec |

#### Source Configuration

Each source must have **either** `Url` **or** `FilePath`:
```json
{
  "Sources": [
    { "Url": "https://api.com/swagger.json" },
    { "FilePath": "./specs/local.yaml" },
    { "Url": "https://api.com/swagger.json" }
  ]
}
```

| Field | Required | Description |
|-------|----------|-------------|
| `Url` | 🔺 Either this or `FilePath` | Remote OpenAPI spec URL |
| `FilePath` | 🔺 Either this or `Url` | Local file path |
| `VirtualPrefix` | No | Prefix all paths *(enables better conflict resolution)* |
| `ExcludePaths` | No | Paths to skip *(supports wildcards!)* |

#### Optional Settings

| Setting | Default |  |
|---------|---------|-------------|
| `Title` | `"My Koalesced API"` | Title for merged spec |
| `OpenApiVersion` | `"3.0.1"` | Target version *(2.0, 3.0.x, 3.1.x, 3.2.x)* |
| `ApiGatewayBaseUrl` | `null` | Gateway URL *(⚠️ rewrites server URLs in spec)* |
| `SkipIdenticalPaths` | `true` | If `false`, throws on duplicate paths |
| `SchemaConflictPattern` | `"{Prefix}{SchemaName}"` | Schema rename pattern |
| `FailOnServiceLoadError` | `false` | If `true`, fails startup on unreachable source |
| `HttpTimeoutSeconds` | `15` | Timeout for fetching remote specs |

#### Cache Settings *(Middleware Only)*

| Setting | Default |  |
|---------|---------|-------------|
| `DisableCache` | `false` | Recomputes spec on every request |
| `AbsoluteExpirationSeconds` | `86400` *(24h)* | Max cache duration |
| `SlidingExpirationSeconds` | `300` *(5min)* | Reset expiration on access |
| `MinExpirationSeconds` | `30` *(30sec)* | The minimum allowed expiration time for caching |

---

## 📝 Configuration Examples

#### Advanced configuration

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
    "SchemaConflictPattern": "{Prefix}_{SchemaName}", // custom pattern 
    "Cache": {
      "AbsoluteExpirationSeconds": 86400,
      "SlidingExpirationSeconds": 300
    }
  }
}
```

#### Strict configuration

```json
{
  "Koalesce": {
    ... 
    "FailOnServiceLoadError": true, // <-----
    "SkipIdenticalPaths": false     // <-----
  }
}
```

#### HttpClient Customization *(Middleware only)*

For custom SSL/TLS, authentication, or retry policies:
```csharp
builder.Services.AddKoalesce(
    configuration,
    configureHttpClient: builder =>
    {
        // Self-signed certificates (dev only!)
        builder.ConfigurePrimaryHttpMessageHandler(() => 
            new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = 
                    (msg, cert, chain, errors) => true
            });

        // Retry policy with Polly
        builder.AddPolicyHandler(GetRetryPolicy());
    });
```

---

## 🔀 Conflict Resolution

### 🟰 Identical Paths

When two services define the same path (e.g., `/api/health`), there's no perfect solution. Koalesce gives you three strategies — each with clear trade-offs:

#### Strategy 1️⃣: VirtualPrefix (Preserve All Paths) ⭐ Recommended
```json
{
  "Sources": [
    { "Url": "https://inventory-api/swagger.json", "VirtualPrefix": "/inventory" },
    { "Url": "https://catalog-api/swagger.json", "VirtualPrefix": "/catalog" }
  ]
}
```

**Result:**
```
Original paths:          Merged spec:
/api/health       →      /inventory/api/health
/api/health       →      /catalog/api/health
```

**✅ Pros:**
- All endpoints preserved.
- No data loss.
- Explicit service boundaries in merged spec.

**⚠️ Cons:**
- **Requires Gateway URL rewrite** (Ocelot, YARP, Kong, etc.).
- Gateway must strip prefix before routing to actual service.
- More configuration needed.

**Use when:** You have a Gateway and want complete API coverage.


#### Strategy 2️⃣: First Source Wins (Default)

```json
{
  "Sources": [
    { "Url": "https://inventory-api/swagger.json" },
    { "Url": "https://catalog-api/swagger.json" }
  ]
}
```

**Result:**
```
Source            Path          Merged spec
Inventory API  →  /api/health → ✅ Included
Catalog API    →  /api/health → ⚠️ Skipped (warning logged)
```

**✅ Pros:**
- Zero Gateway configuration.
- Predictable behavior.
- Works out-of-the-box.

**⚠️ Cons:**
- **Later sources lose conflicting paths**.
- Not suitable if you need all endpoints.
- Health checks, status endpoints often duplicated.

**Use when:** You're okay with losing duplicate paths, or paths are naturally unique


### Strategy 3️⃣: Fail-Fast (Strict Mode)
```json
{
  "Sources": [
    { "Url": "https://inventory-api/swagger.json" },
    { "Url": "https://catalog-api/swagger.json" }
  ],
  "SkipIdenticalPaths": false
}
```

**Result:**
```
❌ KoalesceIdenticalPathFoundException
   Duplicate path detected: /api/health
   Sources: inventory-api, catalog-api
```

**✅ Pros:**
- Forces you to resolve conflicts explicitly.
- Perfect for CI/CD validation.
- No silent data loss.

**⚠️ Cons:**
- Requires upfront path design coordination
- Fails on common paths like `/health`, `/ready`

**Use when:** You want strict contract enforcement or are validating service designs

### 🟰 Identical Schemas

**Automatic Resolution:** When multiple APIs define schemas with identical names (e.g., `Product`), Koalesce automatically renames them using the (customizable) pattern `{Prefix}{SchemaName}`.

**Conflict Behavior:**

| Scenario | Result |
|---|---|
| Both sources have `VirtualPrefix` | **Both** schemas are renamed (e.g., `InventoryProduct`, `CatalogProduct`.) |
| Only one source has `VirtualPrefix` | Only the prefixed source's schema is renamed |
| Neither source has `VirtualPrefix` | First schema keeps original name. Second uses **Sanitized API Title** as prefix. |

> 💡 **Note:** When falling back to the API Title, Koalesce sanitizes the string (PascalCase, alphanumeric only) to ensure valid C# identifiers. For example, `"Sales API v2"` becomes `SalesApiV2`.

**Prefix Priority:**

1. **VirtualPrefix** (if configured): `/inventory` → `InventoryProduct`
2. **API Name** (sanitized): `Koalesce.Samples.InventoryAPI` → `KoalesceSamplesInventoryAPIProduct`

<br/>

### 🤔 Which strategy is the best for you?

Conflicts are an **architectural decision**, not a technical problem. Koalesce makes the trade-offs explicit and lets you choose the strategy that fits your architecture.

**Recommendation:** 
  - Use `VirtualPrefix` with a Gateway for production. 
  - Use `First Wins` for simple scenarios or development. 
  - Use `Fail-Fast` in CI/CD to enforce path uniqueness.

---

## 📜 Links

- [Full CLI Documentation](https://github.com/falberthen/Koalesce/blob/master/docs/cli/README.nuget.md)
- [Koalesce Changelog](https://github.com/falberthen/Koalesce/blob/master/docs/CHANGELOG.md)
- [Koalesce.CLI Changelog](https://github.com/falberthen/Koalesce/tree/master/docs/cli/CHANGELOG.md)

---

## 📧 Support & Contributing

- **Issues**: Report bugs or request features via [GitHub Issues](https://github.com/falberthen/Koalesce/issues).
- **Contributing**: Contributions are welcome! Please read [CONTRIBUTING.md](https://github.com/falberthen/Koalesce/tree/master/docs/CONTRIBUTING.md) before submitting PRs.
- **Sample Projects**: Check out [Koalesce.Samples](https://github.com/falberthen/Koalesce/tree/master/samples) for a complete implementation.

---

## 📝 License

Koalesce is licensed under the [**MIT License**](https://github.com/falberthen/Koalesce/blob/master/LICENSE).

---

> ⚠️ **Migration:** The packages `Koalesce.OpenAPI` and `Koalesce.OpenAPI.CLI` are now deprecated. Please migrate to `Koalesce` and `Koalesce.CLI`.
