# ⚙️ Koalesce - Configuration Reference

Full configuration reference for **Koalesce**. For a quick overview, see the [main README](https://github.com/falberthen/Koalesce#readme).

---

## Required Settings

| Setting | Type | Required | Description |
|---------|------|----------|-------------|
| `Sources` | `array` | 🔺(*Middleware / CLI*) | List of API sources (see below) |
| `MergedEndpoint` | `string` | 🔺(*Middleware*) | HTTP endpoint for merged spec |

---

## Source Configuration

Each source must have **either** `Url` **or** `FilePath`:
```json
{
  "Sources": [
    { "Url": "https://api.com/swagger.json" },
    { "FilePath": "./specs/local.yaml" }
  ]
}
```

| Setting          | Type     | Required | Description |
|------------------|----------|----------|-------------|
| `Url`            | `string` |🔺 Either this or `FilePath` | Remote OpenAPI spec URL |
| `FilePath`       | `string` | 🔺 Either this or `Url` | Local file path  |
| `VirtualPrefix`  | `string` | No | Prefix all paths *(enables better [conflict resolution](CONFLICT-RESOLUTION.md))* |
| `ExcludePaths`   | `array`  | No | Paths to skip *(supports wildcards!)*  |
| `PrefixTagsWith` | `string` | No | Prefix all tags from this source *(e.g., `"Payments"` → `"Payments - Users"`)* |

---

## Optional Settings

| Setting          | Type     | Default | Description |
|------------------|----------|----------|-------------|
| `Info` | [OpenApiInfo](https://learn.microsoft.com/en-us/dotnet/api/microsoft.openapi.openapiinfo) | `object` | Open API Info Object, it provides the metadata about the Open API 
| `OpenApiVersion` | `string` | `"3.0.1"` | Target version *(2.0, 3.0.x, 3.1.x, 3.2.x)*  |
| `ApiGatewayBaseUrl` |  `string` | `null` | Gateway URL *(⚠️ rewrites server URLs in spec)* |
| `SkipIdenticalPaths` |  `boolean`  | `true` | If `false`, throws on duplicate paths  |
| `SchemaConflictPattern` |  `string`  | `"{Prefix}{SchemaName}"` | Schema rename pattern  |
| `FailOnServiceLoadError` |  `boolean` | `false` | If `true`, fails startup on unreachable source |
| `HttpTimeoutSeconds` |  `int`  | `15` | Timeout for fetching remote specs |

#### Merge Report *(Middleware Only)*

| Setting               | Type     | Default | Description                                                                                   |
|-----------------------|----------|---------|-----------------------------------------------------------------------------------------------|
| `MergeReportEndpoint` | `string` | `null`  | Endpoint to serve the merge report. Use `.json` for JSON or `.html` for a formatted HTML page |

> **How the report works:** The report is a **read-only byproduct** of the main merge. Accessing `MergeReportEndpoint` never triggers a new merge — it serves the cached report produced by the last merge on `MergedEndpoint`. If no merge has occurred yet, it returns empty content (`{}` for JSON, or a placeholder page for HTML).

#### Cache Settings *(Middleware Only)*

| Setting          | Type       | Default | Description |
|------------------|------------|----------|-------------|
| `DisableCache`   |  `boolean` |`false`| Recomputes spec on every request |
| `AbsoluteExpirationSeconds`   | `int` | `86400` *(24h)* | Max cache duration |
| `SlidingExpirationSeconds`    | `int` | `300` *(5min)* | Reset expiration on access |
| `MinExpirationSeconds`        | `int` | `30` *(30sec)* | The minimum allowed expiration time for caching |

---

## 📝 Configuration Examples

### Advanced configuration

```json
{
  "Koalesce": {
    "OpenApiVersion": "3.1.0",
    "Info": {
      "Title": "My 🐨Koalesced API",
      "Description": "Unified API aggregating multiple services"
    },
    "Sources": [
      {
        "Url": "https://localhost:8001/swagger/v1/swagger.json",
        "VirtualPrefix": "/customers",
        "PrefixTagsWith": "Customers"
      },
      {
        "Url": "https://localhost:8002/swagger/v1/swagger.json",
        "VirtualPrefix": "/inventory",
        "PrefixTagsWith": "Inventory"
      },
      { "FilePath": "./specs/external-api.json" }
    ],
    "MergedEndpoint": "/swagger/v1/apigateway.json",
    "MergeReportEndpoint": "/koalesce/report",
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
    ...
    "FailOnServiceLoadError": true,
    "SkipIdenticalPaths": false
  }
}
```

---

### Using Koalesce with a Single Source

Koalesce doesn't require multiple sources. When a single source is provided, the same processing pipeline runs — `ExcludePaths`, `PrefixTagsWith`, `OpenApiVersion`, `Info`, and all other options work exactly the same way. The only difference is that no merge or conflict resolution takes place.

This makes Koalesce a practical choice for sanitizing an API spec before publishing it externally, converting it to a different OpenAPI version, or simply cleaning up endpoints and tags — without needing any additional tooling.

> **Tag behavior in sanitization mode:** When using a single source, Koalesce preserves the original tag structure as-is. If the source has no tags, no tags are generated. This differs from merge mode (2+ sources), where Koalesce always ensures operations are tagged for proper grouping in Swagger UI.

```json
{
  "Koalesce": {
    "OpenApiVersion": "3.1.0",
    "Info": {
      "Title": "My Public API",
      "Description": "Clean, public-facing specification"
    },
    "Sources": [
      {
        "Url": "https://localhost:8002/swagger/v1/swagger.json",
        "ExcludePaths": ["/internal/*", "*/admin/*", "/debug/*"],
        "PrefixTagsWith": "v2"
      }
    ],
    "MergedEndpoint": "/swagger/v1/public-api.yaml"
  }
}
```
---

### HttpClient Customization *(Middleware only)*

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
