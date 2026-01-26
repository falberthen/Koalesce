# 🐨 Koalesce

**Koalesce** is a .NET library designed to merge multiple API definitions into a unified document. It enables seamless API Gateway integration and simplifies frontend client generation for microservices-based architectures.

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

**Koalesce** is ideal for **Backend-for-Frontend (BFF)** patterns where external consumers need a unified API view:

- **Frontend applications** consuming an API Gateway.
- **Mobile apps** with a single unified SDK.
- **Third-party developer portals** exposing your APIs.
- **External API consumers** needing consolidated documentation.

> 💡 **Tip:** For internal service-to-service communication, prefer direct service calls with dedicated clients per service to avoid tight coupling and unnecessary Gateway overhead.

---

## Quick Start

### 1. Configure appsettings.json

```json
{
  "Koalesce": {
    "Sources": [
      { "Url": "https://service-a/swagger/v1/swagger.json", "VirtualPrefix": "/sales" },
      { "Url": "https://service-b/swagger/v1/swagger.json", "VirtualPrefix": "/inventory" }
    ],
    "MergedDocumentPath": "/swagger/v1/gateway.json",
    "ApiGatewayBaseUrl": "https://localhost:5000"
  }
}
```

### 2. Register and Enable

```csharp
builder.Services.AddKoalesce(builder.Configuration)
    .ForOpenAPI();

var app = builder.Build();
app.UseKoalesce();
```

## CLI Tool

Install globally to merge OpenAPI specs without hosting an app:

```bash
dotnet tool install --global Koalesce.OpenAPI.CLI --prerelease
koalesce --config ./appsettings.json --output ./gateway.yaml
```

## Conflict Resolution & Governance

**Schema conflicts:** Deterministic renaming strategy to ensure stable client generation.

- **With Prefix:** Sources with `VirtualPrefix` get scoped schemas (e.g., `/inventory` → `Inventory_Product`).
- **No Prefix:** Sources without prefix keep original names (or fallback to API Title if conflicting).
- **Result:** Consistent SDK generation regardless of load order.

**Path conflicts:**

- Use `VirtualPrefix` to preserve all endpoints: `/api/health` → `/inventory/api/health` + `/sales/api/health`
- Set `SkipIdenticalPaths: true` (default) to keep first API's path and skip duplicates.
- Use `ExcludePaths` to exclude specific paths from merge (supports wildcards: `"/api/admin/*"`).

**Resilience:**

- By default, Koalesce skips unreachable services to keep the Gateway alive.
- Set `FailOnServiceLoadError: true` for CI/CD pipelines to ensure all services are reachable before building.

## Documentation

Visit [GitHub Repository](https://github.com/falberthen/Koalesce) for:

- Complete configuration reference
- Caching configuration
- Sample projects with Ocelot integration
- Troubleshooting guide

**Quick Links:**
- [Full Documentation](https://github.com/falberthen/Koalesce#readme)
- [Changelog](https://github.com/falberthen/Koalesce/blob/master/CHANGELOG.md)
- [CLI Changelog](https://github.com/falberthen/Koalesce/tree/master/src/Koalesce.OpenAPI.CLI/CHANGELOG.md)
- [Sample Projects](https://github.com/falberthen/Koalesce/tree/master/samples)

## License

MIT License - see [LICENSE](https://github.com/falberthen/Koalesce/blob/master/LICENSE)