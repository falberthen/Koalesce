# 🐨 Koalesce

**Koalesce** is a .NET library designed to merge multiple OpenAPI definitions into a unified document to enable seamless API Gateway integration and simplify frontend client generation for microservices-based architectures.

---

## How It Works?

- Koalesce fetches OpenAPI definitions from the specified **SourceOpenApiUrls**.
- It then merges them using supported providers, generating a single schema at **MergedOpenApiPath**.
- The final *Koalesced* API definition is serialized and available in `JSON` or `YAML` format.

### ⚡ Features

- ✅ Coalesce multiple OpenAPI definitions into one unified schema.
- ✅ Fully configurable via `appsettings.json`.
- ✅ Aligns perfectly with API Gateways.
- ✅ Aligns perfectly with UI tools for interacting with API endpoints.
- ✅ Allows output a `json` or `yaml` merged document regardless the document type of the source APIs.
- ✅ Streamlines API client generation since it results in one unified schema.
- ✅ Extensible architecture to support new API aggregation strategies.
  

### ⚙️  Basic Configuration
  
| Setting                      | Type       | Default Value | Description |
|------------------------------|-----------|--------------|-------------|
| `SourceOpenApiUrls`         | `array`   | 🔺  | Source list of OpenAPI URLs to merge. |
| `MergedOpenApiPath` | `string`  | 🔺 | Path where the merged API definition is exposed. |
| `Title`      | `string`  | `"My 🐨Koalesced OpenAPI"` | Title for the Koalesced API definition. |
| `SkipIdenticalPaths`     | `boolean` | `true` | If `false`, Koalesce will throw an exception when detecting identical API paths across merged definitions. If `true`, it will log a warning and skip them. |
| `ApiGateWayBaseUrl` | `string` | `null / empty` | If provided, Koalesce will ensure a `single server` for the entire merged document.

- 💡Parameters listed with 🔺 are required.
- 💡The file extension `[.json, .yaml]` defined in **MergedOpenApiPath** will define the merged output format.
- 💡Koalesce respects the order of SourceOpenApiUrls. This affects how identical paths are handled based on the `SkipIdenticalPaths` setting.


```json
{
  "Koalesce": {
    "SourceOpenApiUrls": [
      "https://api1.com/swagger/v1/swagger.json",
      "https://api2.com/swagger/v1/swagger.json"
    ],
    "MergedOpenApiPath": "/swagger/v1/apigateway.json",
    "Title": "My Koalesced API",
    "SkipIdenticalPaths": true,
    "ApiGatewayBaseUrl": "https://api-gateway.com:5000",
    "Cache": {
      "DisableCache": false,
      "AbsoluteExpirationSeconds": 86400,
      "SlidingExpirationSeconds": 300,
      "MinExpirationSeconds": 30
    }
  }
}
```


#### 🛠️ Caching Configuration (`Koalesce.Cache`)

| Setting | Type | Default Value | Description |
|-------------------------------|-----------|--------------|-------------|
| `DisableCache` | `boolean` | `false` | If `true`, Koalesce will **always recompute** the merged OpenAPI document on each request instead of caching it. |
| `AbsoluteExpirationSeconds` | `integer` | `86400` (24h) | The **maximum duration** (in seconds) before the cached OpenAPI document is **forcibly refreshed**, regardless of access. |
| `SlidingExpirationSeconds` | `integer` | `300` (5 min) | **Resets cache expiration** every time the document is accessed. If not accessed within this period, the cache expires earlier than its absolute expiration. |
| `MinExpirationSeconds` | `integer` | `30` | Minimum allowed expiration time (in seconds). Prevents setting an excessively low cache duration that could cause unnecessary recomputation. |

```json
{
  "Koalesce": {
    "Title": "My Koalesced API",
    "SourceOpenApiUrls": [
      "https://api1.com/swagger/v1/swagger.json",
      "https://api2.com/swagger/v1/swagger.json"
    ],
    "MergedOpenApiPath": "/swagger/v1/apigateway.json",    
    "SkipIdenticalPaths": true,
    "ApiGatewayBaseUrl": "https://api-gateway.com",
    "Cache": {
      "DisableCache": false,
      "AbsoluteExpirationSeconds": 86400,
      "SlidingExpirationSeconds": 300,
      "MinExpirationSeconds": 30
    }
  }
}
```


## 📦 Installation

### **🟢 For OpenAPI**

```sh
dotnet add package Koalesce.OpenAPI
```


#### ⚙️ Package-specific Configuration

🔺 This configuration extends the basic settings. Ensure that your Koalesce section includes all required base options.

| Setting                      | Type       | Default Value | Description |
|------------------------------|-----------|--------------|-------------|
| `Koalesce.OpenApiVersion`     | `string`  | "3.0.1"    | OpenAPI version. |

```json
{
  "Koalesce": 
  {
    "OpenApiVersion": "3.0.1",
    "Title": "My Koalesced OpenAPI",
    "SourceOpenApiUrls": [
      "https://localhost:5001/swagger/v1/swagger.json",
      "https://localhost:5002/swagger/v1/swagger.json",
      "https://fakerestapi.azurewebsites.net/swagger/v1/swagger.json"
    ],
    "MergedOpenApiPath": "/swagger/v1/swagger.yaml"
  }
}
```


## 🛠️ Usage with .NET pipeline

### **1️⃣ Register Koalesce.[ForProvider()]**

In `Program.cs`:

```csharp
builder.Services.AddKoalesce()
  // Register Koalesce.OpenAPI provider
  .ForOpenAPI();
```

### **2️⃣ Enable Middleware**

```csharp
app.UseKoalesce();
```


## 🔥 Running an Application (using Swagger.UI)

- Start the application:

   ```sh
   dotnet run
   ```

- Access the **Koalesced API** via Swagger UI:

   ```json
   https://localhost:[port]/swagger/index.html
   ```

- The merged OpenAPI definition should be available at:

   ```json
   https://localhost:[port]/[MergedOpenApiPath]
   ```


## 📝 License & Contribution

**Koalesce** is licensed under the [MIT License](https://github.com/falberthen/Koalesce/blob/master/LICENSE).  
Contributions are welcome! Feel free to submit issues and PRs on GitHub.