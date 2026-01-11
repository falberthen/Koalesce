# 🐨 Koalesce

**Koalesce** is a .NET library designed to merge multiple API definitions into a unified document to enable seamless API Gateway integration and simplify frontend client generation for microservices-based architectures.

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

## 📦 Installation

### 🛠️ For Web Apps (Middleware)

```sh
NuGet\Install-Package Koalesce.OpenAPI
```

### 💻 CLI Tool

```sh
dotnet tool install --global Koalesce.OpenAPI.CLI
```

---

## 🛠️ Quick Start (Middleware)

### 1. Register & Use (`Program.cs`)

```csharp
// Register Services
builder.Services.AddKoalesce(builder.Configuration)
    .ForOpenAPI(options => {
        // Optional: Configure Global Gateway Security
        options.UseJwtBearerGatewaySecurity("Enter your JWT token", "JWT");
    });

var app = builder.Build();

// Enable Middleware
app.UseKoalesce();

app.Run();
```

---

## 💻 Quick Start (CLI)

Use the CLI to generate a static merged file without running the application.

```bash
koalesce --config appsettings.json --output apigateway.yaml --verbose
```

> **⚠️ Note:** When using the CLI, if your `appsettings.json` defines an `ApiGatewayBaseUrl`, you **must** manually include the `GatewaySecurityScheme` section in the JSON file, as the CLI cannot execute C# security configurations.

---

### ⚙️ Configuration (`appsettings.json`)

```json
{
  "Koalesce": {
    "Sources": [
      { "Url": "https://localhost:8001/swagger/v1/swagger.json", "VirtualPrefix": "customers" },
      { "Url": "https://localhost:8002/swagger/v1/swagger.json", "VirtualPrefix": "inventory" }
    ],
    "MergedDocumentPath": "/swagger/v1/apigateway.json",
    "Title": "My Koalesced API",
    "OpenApiVersion": "3.0.1",
    "ApiGatewayBaseUrl": "https://localhost:5000",
    "GatewaySecurityScheme": {
      "Type": "Http",
      "Scheme": "bearer",
      "BearerFormat": "JWT",
      "Description": "JWT Authorization header using the Bearer scheme."
    }
  }
}
```

---

## 🔗 Links

* [**Documentation & Source Code**](https://github.com/falberthen/Koalesce)
* [**Changelog**](https://github.com/falberthen/Koalesce/blob/master/CHANGELOG.md)
* [**License (MIT)**](https://github.com/falberthen/Koalesce/blob/master/LICENSE)