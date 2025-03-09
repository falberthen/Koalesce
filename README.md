
![CI Status](https://github.com/falberthen/Koalesce/actions/workflows/tests.yml/badge.svg)


# 🐨 Koalesce

Koalesce is a **.NET distributed library** that *coalesces* multiple **OpenAPI definitions** into a single document. This allows seamless **API Gateway integration** and simplifies **frontend client generation**.

---

## ⚡ Features

- ✅ Coalesce multiple OpenAPI definitions into one unified schema.
- ✅ Fully configurable via `appsettings.json`.
- ✅ Aligns perfectly with API Gateways.
- ✅ Aligns perfectly with UI tools for interacting with API endpoints.
- ✅ Allows output a `json` or `yaml` merged document regardless the document type of the source APIs.
- ✅ Streamlines API client generation since it results in one unified schema.
- ✅ Extensible architecture to support new API aggregation strategies.
  
<br/>

#### ⚙️ 🔺 Basic Configuration

| Setting                      | Type       | Default Value | Description |
|------------------------------|-----------|--------------|-------------|
| `Koalesce.SourceOpenApiUrls`         | `array`   | 🔺  | Source list of OpenAPI URLs to merge. |
| `Koalesce.MergedOpenApiPath` | `string`  | 🔺 | Path where the merged API definition is exposed. |
| `Koalesce.Title`      | `string`  | "My 🐨Koalesced OpenAPI" | Title for the Koalesced API definition. |
| `Koalesce.CacheDurationSeconds`      | `int`  | 300 | The cache duration in seconds for a Koalesced API definition. |
| `Koalesce.DisableCache` | `boolean` | `false` | Disables caching when set to `true`. This may impact performance by forcing recomputation on every request. |
| `Koalesce.ApiGateWayBaseUrl` | `string` | null / empty | If provided, Koalesce will ensure a single server for the entire merged document. 
<br/>

**💡Note:** The file extension defined in **MergedOpenApiPath** will define the merged output format.

**💡Note:** Koalesce will respect the order defined in **SourceOpenApiUrls**.


<br/>

## 📦 Installation

### **🟢 For OpenAPI**
![NuGet](https://img.shields.io/nuget/vpre/Koalesce.OpenAPI.svg)
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
<br/>

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

<br/>

## 🔄 How It Works (OpenAPI providers)

- Koalesce fetches OpenAPI definitions from specified **SourceOpenApiUrls**.
- Using a merging mechanism, it uses supported providers to coalesce them into a single schema into specified **MergedOpenApiPath**. 
- The *Koalesced* API definition is serialized following the path and format *(json and yaml supported)*.

<br/>

## 🔥 Running an Application (using Swagger.UI)
-  Start the application:
   ```sh
   dotnet run
   ```
-  Access the **Koalesced API** via Swagger UI:
   ```
   https://localhost:5000/swagger/index.html
   ```
- The merged OpenAPI definition should be available at:
   ```
   https://localhost:5000/swagger/v1/swagger.json
   ```

<br/>

## 🛠 Extending Koalesce
You can create **custom Koalesce providers** by implementing `IKoalesceProvider`.

*Example:*
```csharp
public class CustomKoalesceProvider : IKoalesceProvider
{
    public async Task<string> ProvideSerializedDocumentAsync()
    {
        return "{}"; // Example: Return empty OpenAPI document
    }
}
```
*Then, register it:*
```csharp
builder.Services.AddKoalesce(builder.Configuration)
  .AddProvider<DummyProvider, KoalesceProviderOptions>();
```

<br/>


## ⚠️ Important Considerations and Limitations

🔹 What Happens when using Koalesce?

### 🔐 Security Schemes & Authorization
Koalesce merges authentication schemes found in different API definitions. If multiple APIs define different security schemes (e.g., OAuth2, API Key, Bearer Tokens), 
these will be preserved in the final Koalesced API document.

- ⚠️ Each API's operations retain their respective security requirements, ensuring that authorization logic remains per API group.
- ⚠️ When using tools like Swagger UI, the Authorize prompt will display authentication inputs for **all security schemes found across the merged document**.


### 🔀 Handling Identical Routes
The OpenAPI specification does not allow duplicate paths, meaning that if multiple APIs define identical paths (e.g., /api/products exists in multiple APIs), 
they will be merged into a single entry.

- ⚠️ **The order of SourceOpenApiUrls** determines which API takes merging precedence.
- ⚠️ **Only one path definition will be retained in the Koalesced document**, as OpenAPI does not support multiple definitions for the same path.

🔹 How to Avoid This?

Since this is an OpenAPI constraint, possible solutions include:

- ✅ Ensure unique routes across APIs before merging.
- ✅ Use API-specific servers in Swagger UI to differentiate endpoints.
- ✅ Restructure APIs if merging them into a single OpenAPI document is necessary.

<br/>

## 📝 License
Koalesce is licensed under the **MIT License**.

## ❤️ Contributing
Contributions are welcome! Feel free to open issues and submit PRs.

## 📧 Contact
For support or inquiries, reach out via **GitHub Issues**.