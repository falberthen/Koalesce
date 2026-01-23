# 🐨 Koalesce

**Koalesce** is a .NET library designed to merge multiple API definitions into a unified document. It enables seamless API Gateway integration and simplifies frontend client generation for microservices-based architectures.

## ⚡ Key Features

- ✅ **Merge Multiple APIs**: Coalesce multiple API definitions into one unified schema.
- ✅ **Flexible Security**: Apply global Gateway security OR preserve downstream API security configurations.
- ✅ **Conflict Resolution**: Deterministic schema renaming and path collision detection.
- ✅ **Resilience vs. Strictness**: Choose between fault-tolerant production modes or strict validation for CI/CD.
- ✅ **Configurable Caching**: Fine-grained cache control with absolute/sliding expiration settings.
- ✅ **Gateway Integration**: Works seamlessly with **Ocelot**, **YARP**, and other API Gateways.
- ✅ **Client Generation**: Streamlines API client generation (e.g., **NSwag**, **Kiota**) with a single unified schema.
- ✅ **Flexible Configuration**: Configure via `appsettings.json` or Fluent API.
- ✅ **Format Agnostic Output**: Output `JSON` or `YAML` regardless of source document format.
- ✅ **Multi-targeting**: Native support for **.NET 8.0 (LTS)** and **.NET 10.0**.
- ✅ **Extensible Core**: Designed to support future providers for other API specification formats.

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
    "ApiGatewayBaseUrl": "https://localhost:5000",
    "FailOnServiceLoadError": false
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

### 3. Optional: Configure Global Security

```csharp
builder.Services.AddKoalesce(builder.Configuration)
    .ForOpenAPI(options =>
    {
        // Override all downstream security with Gateway auth
        options.ApplyGlobalJwtBearerSecurityScheme(
            schemeName: "Bearer",
            description: "Enter your JWT token"
        );
    });
```

**Note:** If you don't configure global security, Koalesce preserves each downstream API's security as-is.

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
- Security options and extension methods
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