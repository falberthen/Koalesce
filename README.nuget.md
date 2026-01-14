# 🐨 Koalesce

**Koalesce** is a .NET library designed to merge multiple API definitions into a unified document. It enables seamless API Gateway integration and simplifies frontend client generation for microservices-based architectures.

---

## How It Works?

- Koalesce fetches API definitions from the specified **Sources**.
- It then merges them using supported providers, generating a single schema at **MergedDocumentPath**.
- The final *Koalesced* API definition is serialized and available in `JSON` or `YAML` format.

### ⚡ Key Features

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

### 📦 Installation

#### Koalesce for OpenAPI Middleware (ASP.NET Core)

```shell
dotnet add package Koalesce.OpenAPI
```

#### 🟢 Koalesce.OpenAPI.CLI as a Global Tool

```shell
dotnet tool install --global Koalesce.OpenAPI.CLI
```

---

### Configuration

**1. Configure `appsettings.json`**

```json
"Koalesce": {
  "Sources": [
    { "Url": "https://service-a/swagger/v1/swagger.json" },
    { "Url": "https://service-b/swagger/v1/swagger.json" }
  ],
  "MergedDocumentPath": "/swagger/v1/gateway.json",
  "OpenApiVersion": "3.0.1",
  "ApiGatewayBaseUrl": "https://localhost:5000"
}
```

**2. Register Services**

```csharp
// Program.cs
builder.Services.AddKoalesce(builder.Configuration)
    .ForOpenAPI(options => 
    {
        // Optional: Configure Gateway Security (e.g., JWT)
        options.UseJwtBearerGatewaySecurity(
            description: "Enter your JWT token",
            bearerFormat: "JWT"
        );
    });
```

**3. Enable Middleware**

```csharp
var app = builder.Build();
app.UseKoalesce();
app.Run();
```

---

### 💻 Using Koalesce.OpenAPI through CLI (Command Line Interface)

The `Koalesce.OpenAPI.CLI` tool was built specifically to allow the usage of Koalesce for merging OpenAPI definitions directly into a file in the disk, without the need for a .NET application hosting the middleware.

```bash
koalesce --config ./appsettings.json --output ./gateway.yaml --verbose
```

---

### 📚 Documentation

For a comprehensive guide and advanced configuration options, please visit the [**GitHub Repository**](https://github.com/falberthen/Koalesce).