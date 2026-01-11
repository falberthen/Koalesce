![CI Status](https://github.com/falberthen/Koalesce/actions/workflows/tests.yml/badge.svg)

# 🐨 Koalesce

**Koalesce** is a .NET library designed to merge multiple API definitions into a unified document to enable seamless API Gateway integration and simplify frontend client generation for microservices-based architectures.

---

## How It Works?

- Koalesce fetches API definitions from the specified **Sources**.
- It then merges them using supported providers, generating a single schema at **MergedDocumentPath**.
- The final *Koalesced* API definition is serialized and available in `JSON` or `YAML` format.

### ⚡ Features

- ✅ Coalesce multiple API definitions into one unified schema.
- ✅ **Agnostic Core:** Designed to support future formats beyond OpenAPI.
- ✅ **Fail-Fast Validation:** Validates URLs and paths at startup to prevent runtime errors.
- ✅ **Gateway Security Integration:** Define a single authentication source of truth for your API Gateway.
- ✅ Fully configurable via `appsettings.json` or Fluent API.
- ✅ Aligns perfectly with API Gateways (**Ocelot**, **YARP**).
- ✅ Allows output a `json` or `yaml` merged document regardless the document type of the source APIs.
- ✅ Streamlines API client generation (e.g., **NSwag**, **Kiota**) since it results in one unified schema.
- ✅ **Multi-targeting:** Native support for **.NET 8.0 (LTS)** and **.NET 10.0**.

---

## ⚙️ Configuration

Koalesce configuration is divided into **Core Options** and **Provider Options** (e.g., OpenAPI).

### 1️⃣ Core Configuration (`Koalesce`)

| Setting | Type | Default Value | Description |
|---|---|---|---|
| `Sources` | `array` | 🔺 | List of API sources. Each item contains `Url` and **optional** `VirtualPrefix`. |
| `MergedDocumentPath` | `string` | 🔺 | Path where the merged API definition is exposed. |
| `Title` | `string` | `"My 🐨Koalesced API"` | Title for the Koalesced API definition. |
| `SkipIdenticalPaths` | `boolean` | `true` | If `false`, throws exception on duplicate paths. If `true`, logs warning and skips duplicates. |

- 💡 Parameters listed with 🔺 are required.
- 💡 The file extension `[.json, .yaml]` defined in **MergedDocumentPath** determines the output format.

### 2️⃣ OpenAPI Provider Configuration

These settings are specific to the `Koalesce.OpenAPI` provider.

| Setting | Type | Default Value | Description |
|---|---|---|---|
| `OpenApiVersion` | `string` | "3.0.1" | Target OpenAPI version for the output. |
| `ApiGatewayBaseUrl` | `string` | `null` | The public URL of your Gateway. **Requires** a Security Scheme if set. |
| `GatewaySecurityScheme` | `object` | `null` | Defines the global security scheme (e.g., JWT, ApiKey) for the Gateway. |
| `IgnoreGatewaySecurity` | `boolean` | `false` | If `true`, keeps downstream security schemes instead of enforcing the Gateway scheme. |

---

### 📝 Example `appsettings.json`

```json
{
  "Koalesce": {
    // Core Settings
    "Sources": [
      {
        "Url": "https://localhost:8001/swagger/v1/swagger.json",
        "VirtualPrefix": "customers"
      },
      {
        "Url": "https://localhost:8002/swagger/v1/swagger.json",
        "VirtualPrefix": "inventory"
      }
    ],
    "MergedDocumentPath": "/swagger/v1/apigateway.json",
    "Title": "My Koalesced API",
    
    // OpenAPI Specific Settings
    "OpenApiVersion": "3.0.1",
    "ApiGatewayBaseUrl": "https://localhost:5000",
    
    // Caching
    "Cache": {
      "DisableCache": false,
      "AbsoluteExpirationSeconds": 86400
    }
  }
}
```

> **Note on `VirtualPrefix`:** When you define a prefix (e.g., `"inventory"`), Koalesce modifies the path in the documentation (e.g., `/api/get` becomes `/inventory/api/get`). Your API Gateway (e.g., Ocelot/YARP) must be configured to route this prefixed path back to the original downstream service.

---

## 🔐 Security Configuration

Koalesce supports a **Global Gateway Security**. You can configure this in two ways:

#### A. Fluent API (Recommended for Web Apps)

Koalesce provides a set of fluent extension methods to easily configure common security scenarios. This keeps your `appsettings.json` clean and leverages C# type safety.

**Available Extension Methods:**

| Method | Description |
|---|---|
| `UseJwtBearerGatewaySecurity` | Configures standard JWT Bearer Token authentication. |
| `UseApiKeyGatewaySecurity` | Configures API Key authentication (Header or Query). |
| `UseBasicAuthGatewaySecurity` | Configures HTTP Basic Authentication. |
| `UseOAuth2ClientCredentialsGatewaySecurity` | Configures OAuth2 Client Credentials flow. |
| `UseOAuth2AuthCodeGatewaySecurity` | Configures OAuth2 Authorization Code flow. |
| `UseOpenIdConnectGatewaySecurity` | Configures OIDC via Discovery Document. |

**Usage Example:**

```csharp
builder.Services.AddKoalesce(builder.Configuration)
    .ForOpenAPI(options =>
    {
        // Example 1: JWT Bearer (Most Common)
        options.UseJwtBearerGatewaySecurity(
            description: "Enter your JWT token",
            bearerFormat: "JWT"
        );
        
        /* Other examples:
        
        // Example 2: API Key
        options.UseApiKeyGatewaySecurity("X-Api-Key", ApiKeyLocation.Header);

        // Example 3: OAuth2 Client Credentials
        options.UseOAuth2ClientCredentialsGatewaySecurity(
            tokenUrl: "[https://auth.example.com/connect/token](https://auth.example.com/connect/token)",
            scopes: new Dictionary<string, string> { { "api.read", "Read Access" } }
        );

        // Example 4: OpenID Connect (OIDC)
        options.UseOpenIdConnectGatewaySecurity(
            openIdConnectUrl: "[https://auth.example.com/.well-known/openid-configuration](https://auth.example.com/.well-known/openid-configuration)",
            openIdConnectUrlDescription: "Discovery Endpoint"
        );
        */
    });
```

> 💡 Check the **Koalesce.Samples.Swagger.Ocelot** project for a complete implementation of all these scenarios.

#### B. JSON Configuration (Required for CLI)

If you use the **CLI tool**, you **must** define the security scheme in `appsettings.json`, as the CLI cannot execute your C# startup code.

```json
"Koalesce": {
  "ApiGatewayBaseUrl": "https://localhost:5000",
  "GatewaySecurityScheme": {
    "Type": "Http",
    "Scheme": "bearer",
    "BearerFormat": "JWT",
    "Description": "JWT Authorization header using the Bearer scheme."
  }
}
```

---

## 📦 Installation

#### 🟢 Koalesce for OpenAPI Middleware

![NuGet](https://img.shields.io/nuget/vpre/Koalesce.OpenAPI.svg)

```sh
# Package Manager
NuGet\Install-Package Koalesce.OpenAPI -Version 1.0.0-alpha.3
```
```sh
# .NET CLI
dotnet add package Koalesce.OpenAPI --version 1.0.0-alpha.3
```

#### 🟢 Koalesce.OpenAPI.CLI

![NuGet](https://img.shields.io/nuget/vpre/Koalesce.OpenAPI.CLI.svg)

To install the **Koalesce.OpenAPI.CLI** globally:

```bash
dotnet tool install --global Koalesce.OpenAPI.CLI --version 1.0.0-alpha.3
```

---

## 🛠️ Using with .NET pipeline

#### 1️⃣ Register Koalesce.[ForProvider()]

```csharp
builder.Services.AddKoalesce(builder.Configuration)
    .ForOpenAPI(); // Add options lambda here for fluent security config
```

#### 2️⃣ Enable Middleware

```csharp
app.UseKoalesce();
```

---

## 💻 Using with Command Line Interface (CLI)

#### Arguments:

- 🔺`--config` specifies the path to your `appsettings.json`.
- 🔺`--output` defines the path for the merged OpenAPI spec file.
- `--verbose` enables detailed logging.
- `--version` displays the current version.

#### Example

```bash
koalesce --config ./config/appsettings.json --output ./merged-specs/apigateway.yaml --verbose
```

> **⚠️ CLI & Security:** If your `appsettings.json` defines an `ApiGatewayBaseUrl`, you **must** also provide a `GatewaySecurityScheme` section within the JSON file. The CLI validates this relationship strictly to ensure the generated document is valid.

<br/>
<img src="img/Screenshot_CLI.png"/>

---

## ⚠️ Important Considerations and Limitations

#### 🔐 Security Schemes & Authorization

Koalesce enforces a "Single Source of Truth" for Gateway security when `ApiGatewayBaseUrl` is set.
- By default, downstream security schemes are **removed** and replaced by the `GatewaySecurityScheme`.
- If you need to keep downstream schemes, set `"IgnoreGatewaySecurity": true`.

#### 🔀 Handling Identical Routes

If two or more microservices share the same route (e.g., `/api/health`), a collision occurs.

🔹 **How to resolve this?**

1.  **Use `VirtualPrefix` (Recommended):**
    Assign a unique prefix in `appsettings.json` (e.g., `inventory`, `products`).
    - Koalesce transforms `/api/health` into `/inventory/api/health`.
    - This ensures unique paths in the documentation.
    - *Requires API Gateway URL Rewrite configuration.*

2.  **Order of Precedence:**
    - If `VirtualPrefix` is not used, the **order of Sources** determines precedence.
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