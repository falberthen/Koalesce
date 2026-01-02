
![CI Status](https://github.com/falberthen/Koalesce/actions/workflows/tests.yml/badge.svg)

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
  
---
### ⚙️ Basic Configuration
  
| Setting                      | Type       | Default Value | Description |
|------------------------------|-----------|--------------|-------------|
| `SourceOpenApiUrls`         | `array`   | 🔺  | Source list of OpenAPI URLs to merge. |
| `MergedOpenApiPath` | `string`  | 🔺 | Path where the merged API definition is exposed. |
| `Title`      | `string`  | `"My 🐨Koalesced OpenAPI"` | Title for the Koalesced API definition. |
| `SkipIdenticalPaths`     | `boolean` | `true` | If `false`, Koalesce will throw an exception when detecting identical API paths across merged definitions. If `true`, it will log a warning and skip them. |
| `ApiGateWayBaseUrl` | `string` | `null / empty` | If provided, Koalesce will ensure a `single server` for the entire merged document.

- 💡Parameters listed with 🔺 are required.
- 💡The file extension `[.json, .yaml]` defined in **MergedOpenApiPath** will define the merged output format.
- 💡Koalesce respects the order of **SourceOpenApiUrls**. This affects how identical paths are handled based on the `SkipIdenticalPaths` setting.

<br>

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

#### ⚙️ Koalesce.OpenAPI Configuration

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

---

## 📦 Installation

### **🟢 Koalesce for OpenAPI** Middleware

![NuGet](https://img.shields.io/nuget/vpre/Koalesce.OpenAPI.svg)

```sh
# Package Manager
[.NET 10.0] NuGet\Install-Package Koalesce.OpenAPI -Version 1.0.0-alpha.1
[.NET 8.0]  NuGet\Install-Package Koalesce.OpenAPI -Version 0.1.1-alpha.2
```
```sh
# .NET CLI
[.NET 10.0] dotnet add package Koalesce.OpenAPI --version 1.0.0-alpha.1
[.NET 8.0]  dotnet add package Koalesce.OpenAPI --version 0.1.1-alpha.2
```


### **🟢 Koalesce.OpenAPI.CLI**

![NuGet](https://img.shields.io/nuget/vpre/Koalesce.OpenAPI.CLI.svg)

To install the **Koalesce.OpenAPI.CLI** globally, use the following command:

```bash
[.NET 10.0] dotnet tool install --global Koalesce.OpenAPI.CLI --version 1.0.0-alpha.1
[.NET 8.0]  dotnet tool install --global Koalesce.OpenAPI.CLI --version 0.1.1-alpha.1

```

To update the tool to the latest version:

```bash
dotnet tool update --global Koalesce.OpenAPI.CLI
```

---

## 🛠️ Using with .NET pipeline

#### **1️⃣ Register Koalesce.[ForProvider()]**

```csharp
builder.Services.AddKoalesce()
  // Register Koalesce.OpenAPI provider
  .ForOpenAPI();
```

#### **2️⃣ Enable Middleware**

```csharp
app.UseKoalesce();
```

<br>

## 🛠️ Using with Command Line Interface (CLI)

#### **Basic Command Structure**

```bash
koalesce --config <path-to-appsettings.json> --output <path-to-output-spec>
```

#### **Example**

```bash
koalesce --config ./config/appsettings.json --output ./merged-specs/apigateway.yaml
```

In this example:

- `--config` specifies the path to your `appsettings.json` configuration file with Koalesce settings.
- `--output` defines the path where the merged OpenAPI specification file will be saved.

---

## ⚠️ Important Considerations and Limitations

### 🔐 Security Schemes & Authorization

Koalesce merges authentication schemes found in different API definitions. If multiple APIs define different security schemes (e.g., OAuth2, API Key, Bearer Tokens),
these will be preserved in the final Koalesced API document.

- ⚠️ Each API's operations retain their respective security requirements, ensuring that authorization logic remains per API group.
- ⚠️ When using tools like Swagger UI, the Authorize prompt will display authentication inputs for **all security schemes found across the merged document**.

### 🔀 Handling Identical Routes

🔹 At the moment, what Happens when using Koalesce?

- ⚠️ **The order of SourceOpenApiUrls** determines which API takes merging precedence.
- ⚠️ By default, Koalesce is configured with **SkipIdenticalPaths** set to `true`, meaning it will ignore duplicate paths, keeping only the first occurrence.
If set to `false`, Koalesce will throw an exception when detecting identical paths across merged APIs.
- ⚠️ **Only one path definition will be retained in the Koalesced document**, as OpenAPI does not support multiple definitions for the same path.

🔹 How to Avoid This?

- ✅ Ensure unique routes across APIs before merging.
- ✅ Use API-specific servers in Swagger UI to differentiate endpoints.
- ✅ Restructure APIs if merging them into a single OpenAPI document is necessary.

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