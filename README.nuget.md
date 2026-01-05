# 🐨 Koalesce

**Koalesce** is a .NET library designed to merge multiple OpenAPI definitions into a unified document to enable seamless API Gateway integration and simplify frontend client generation for microservices-based architectures.

---

## How It Works?

- Koalesce fetches OpenAPI definitions from the specified **OpenApiSources**.
- It then merges them using supported providers, generating a single schema at **MergedOpenApiPath**.
- The final *Koalesced* API definition is serialized and available in `JSON` or `YAML` format.

### ⚡ Features

- ✅ Coalesce multiple OpenAPI definitions into one unified schema.
- ✅ Fully configurable via `appsettings.json`.
- ✅ Aligns perfectly with API Gateways (Ocelot, YARP).
- ✅ Allows output a `json` or `yaml` merged document regardless the document type of the source APIs.
- ✅ Streamlines API client generation since it results in one unified schema.
- ✅ Extensible architecture to support new API aggregation strategies.
- ✅ **Multi-targeting:** Native support for **.NET 8.0 (LTS)** and **.NET 10.0**.

---
#### ⚙️ Basic Configuration

| Setting | Type | Default Value | Description |
|---|---|---|---|
| `OpenApiSources` | `array` | 🔺 | List of API sources. Each item contains `Url` and **optional** `VirtualPrefix`. |
| `MergedOpenApiPath` | `string` | 🔺 | Path where the merged API definition is exposed. |
| `Title` | `string` | `"My 🐨Koalesced OpenAPI"` | Title for the Koalesced API definition. |
| `SkipIdenticalPaths` | `boolean` | `true` | If `false`, throws exception on duplicate paths. If `true`, logs warning and skips duplicates. |
| `ApiGatewayBaseUrl` | `string` | `null` | If provided, ensures a `single server` URL for the merged document (essential for "Try it out"). |

- 💡 Parameters listed with 🔺 are required.
- 💡 The file extension `[.json, .yaml]` defined in **MergedOpenApiPath** determines the output format.

<br>

```json
{
  "Koalesce": {
    "OpenApiSources": [
      {
        "Url": "https://localhost:8001/swagger/v1/swagger.json",
        "VirtualPrefix": "customers"
      },
      {
        "Url": "https://localhost:8002/swagger/v1/swagger.json",
        "VirtualPrefix": "inventory"
      }
    ],
    "MergedOpenApiPath": "/swagger/v1/apigateway.json",
    "Title": "My Koalesced API",
    "SkipIdenticalPaths": true,
    "ApiGatewayBaseUrl": "https://localhost:5000",
    "Cache": {
      "DisableCache": false,
      "AbsoluteExpirationSeconds": 86400,
      "SlidingExpirationSeconds": 300,
      "MinExpirationSeconds": 30
    }
  }
}
```

> **Note on `VirtualPrefix`:** When you define a prefix (e.g., `"customers"`), Koalesce modifies the path in the documentation (e.g., `/api/get` becomes `/customers/api/get`).
> **Important:** Your API Gateway (e.g., Ocelot/YARP) must be configured to route this prefixed path back to the original downstream service.

---
#### 🛠️ Caching Configuration (`Koalesce.Cache`)

| Setting | Type | Default Value | Description |
|---|---|---|---|
| `DisableCache` | `boolean` | `false` | If `true`, recomputes the document on every request. |
| `AbsoluteExpirationSeconds` | `integer` | `86400` (24h) | Max duration before forced refresh. |
| `SlidingExpirationSeconds` | `integer` | `300` (5 min) | Resets expiration on access. |
| `MinExpirationSeconds` | `integer` | `30` | Minimum allowed expiration time. |
---
#### ⚙️ Koalesce.OpenAPI Configuration

🔺 This configuration extends the basic settings.

| Setting | Type | Default Value | Description |
|---|---|---|---|
| `Koalesce.OpenApiVersion` | `string` | "3.0.1" | Target OpenAPI version for the output. |

```json
{
  "Koalesce": {
    "OpenApiVersion": "3.0.1",    
    // ... same other configurations
  }
}
```

---

## 🛠️ Using with .NET pipeline

#### 1️⃣ Register Koalesce.[ForProvider()]

```csharp
builder.Services.AddKoalesce()
  .ForOpenAPI(); // Register Koalesce.OpenAPI provider
```

#### 2️⃣ Enable Middleware

```csharp
app.UseKoalesce();
```

---
## 🛠️ Using with Command Line Interface (CLI)

#### Arguments:

- 🔺`--config` specifies the path to your `appsettings.json`.
- 🔺`--output` defines the path for the merged OpenAPI spec file.
- `--verbose` enables detailed logging.
- `--version` displays the current version.

#### Example

```bash
koalesce --config ./config/appsettings.json --output ./merged-specs/apigateway.yaml
```

---

## ⚠️ Important Considerations and Limitations

#### 🔐 Security Schemes & Authorization

Koalesce merges authentication schemes found in different API definitions.
- ⚠️ Each API's operations retain their respective security requirements.
- ⚠️ When using Swagger UI, the Authorize prompt will display inputs for **all security schemes** found across the merged document.

#### 🔀 Handling Identical Routes

If two or more microservices share the same route (e.g., `/api/health`), a collision occurs.

🔹 **How to resolve this?**

1.  **Use `VirtualPrefix` (Recommended):**
    Assign a unique prefix in `appsettings.json` (e.g., `inventory`, `products`).
    - Koalesce transforms `/api/health` into `/inventory/api/health`.
    - This ensures unique paths in the documentation.
    - *Requires API Gateway URL Rewrite configuration.*

2.  **Order of Precedence:**
    - If `VirtualPrefix` is not used, the **order of OpenApiSources** determines precedence.
    - By default (`SkipIdenticalPaths: true`), duplicates are ignored (first wins).

---

#### 📝 License

Koalesce is licensed under the [**MIT License**](https://github.com/falberthen/Koalesce/blob/master/LICENSE).

#### ❤️ Contributing

Contributions are welcome! Feel free to open issues and submit PRs.

#### 📧 Contact

For support or inquiries, reach out via **GitHub Issues**.

#### 📜 Koalesce Changelog

See the full changelog [here](https://github.com/falberthen/Koalesce/blob/master/CHANGELOG.md).

#### 📜 Koalesce.OpenAPI.CLI Changelog

See the full changelog [here](https://github.com/falberthen/Koalesce/tree/master/src/Koalesce.OpenAPI.CLI/CHANGELOG.md).