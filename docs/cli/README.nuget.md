# Koalesce CLI Tool

![Koalesce](https://raw.githubusercontent.com/falberthen/Koalesce/master/img/koalesce_small.png)

**Koalesce.CLI** is a standalone command-line tool that uses [Koalesce](https://github.com/falberthen/Koalesce#readme) to merge multiple OpenAPI definitions into a single unified API specification, and save it to a file on disk.

![.NET](https://img.shields.io/badge/.NET-8-512BD4?logo=dotnet) ![.NET](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet) [![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

⭐ **If you find Koalesce useful, please consider giving it a star on [GitHub](https://github.com/falberthen/Koalesce)!**

---

## The Problem

Building microservices or modular APIs? You're probably dealing with:

- 🔀 Frontend teams juggling **multiple Swagger UIs** across services.
- 📚 Scattered API documentation with no **unified view for consumers**.
- 🛠️ Client SDK generation hitting **10+ different endpoints**.

---

## The Solution

```
  ┌──────────────────┐
  │  Service A       │──┐
  │  /swagger.json   │  │
  └──────────────────┘  │
                        │             🐨
  ┌──────────────────┐  │      ┌──────────────┐      ┌─────────────────────┐
  │  Service B       │──┼─────>│   Koalesce   │─────>│  Unified OpenAPI    │
  │  /openapi.yaml   │  │      └──────────────┘      │  • One Swagger UI   │
  └──────────────────┘  │                            │  • One SDK client   │
                        │                            │  • One docs portal  │
  ┌──────────────────┐  │                            └─────────────────────┘
  │  Service C       │──┘
  │  local/spec.json │
  └──────────────────┘
```

**Koalesce** fetches OpenAPI definitions from multiple sources (URLs or files), resolves conflicts, and outputs a single unified definition.

---

## How It Works

```
  ┌────────────────────────────────────────────────────────────────┐
  │ 1. FETCH APIS                                                  │
  │    • Read from URLs (https://api.com/swagger.json)             │
  │    • Read from files (./specs/local.yaml)                      │
  │    • Support OpenAPI 2.0, 3.0.x, 3.1.x, 3.2.x                  │
  │    • Support JSON and YAML formats                             │
  └────────────────────────────────────────────────────────────────┘
                                ↓
  ┌────────────────────────────────────────────────────────────────┐
  │ 2. RESOLVE CONFLICTS                                           │
  │                                                                │
  │    Path Conflicts (You Choose):                                │
  │    A) VirtualPrefix → /inventory/health + /catalog/health      │
  │    B) First Wins    → /health (from first source only)         │
  │    C) Fail-Fast     → Throw exception on collision             │
  │                                                                │
  │    Schema Conflicts (Auto-rename):                             │
  │    • Inventory.Product → InventoryProduct                      │
  │    • Catalog.Product   → CatalogProduct                        │
  │                                                                │
  └────────────────────────────────────────────────────────────────┘
                                ↓
  ┌────────────────────────────────────────────────────────────────┐
  │ 3. OUTPUT                                                      │
  │    • Single OpenAPI spec (JSON or YAML)                        │
  │    • Target version: 2.0, 3.0.x, 3.1.x, or 3.2.x               │
  │    • Ready for Swagger UI, Scalar, Kiota, NSwag                │
  └────────────────────────────────────────────────────────────────┘
```

---

## 📦 Quick Start

### 1️⃣ Install

```bash
# Koalesce as a CLI standalone tool
dotnet tool install --global Koalesce.CLI --prerelease
```

### 2️⃣ Configure

```json
// your .json file
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
    ]
  }
}
```

### 3️⃣ Run it

```bash
  koalesce -c .\appsettings.json -o .\Output\apigateway.yaml
```

![Koalesce CLI Screenshot](https://raw.githubusercontent.com/falberthen/Koalesce/master/img/Screenshot_CLI_Sample.png)



---
## CLI arguments

| Option       | Shortcut   | Required |                                                  |
| ------------ | ---------- | -------- | ----------------------------------------------------------- |
| `--config`   | `-c`       | 🔺Yes   | Path to your configuration `.json` file.                    |
| `--output`   | `-o`       | 🔺Yes   | Path for the merged OpenAPI spec file.                      |
| `--insecure` | `-k`, `-i` | No       | Skip SSL certificate validation (for self-signed certs).    |
| `--verbose`  |            | No       | Enable detailed logging.                                    |
| `--version`  |            | No       | Display current version.                                    |

💡 The CLI merges OpenAPI definitions directly into a file on disk without requiring a host application.

---

## ⚙️ Configuration Reference

#### Required Settings

| Setting | Type | Required |   |
|---------|---------|-------------|---|
| `Sources` | `array` | 🔺Yes | List of API sources (see below) |

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

- [Full Koalesce Documentation](https://github.com/falberthen/Koalesce/blob/master/docs/README.md)
- [Koalesce.CLI Changelog](https://github.com/falberthen/Koalesce/tree/master/docs/cli/CHANGELOG.md)
- [Koalesce Changelog](https://github.com/falberthen/Koalesce/blob/master/docs/CHANGELOG.md)

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
