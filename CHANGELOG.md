# Changelog

All notable changes to this project will be documented in this file.
> This changelog follows the [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) format and adheres to [Semantic Versioning](https://semver.org/).

---

## [1.0.0-alpha.5] - 2026-01-15

### Added

- **NuGet Metadata:** Added official project icon.

---

## [1.0.0-alpha.4] - 2026-01-13

### Changed

- **Defensive Cache Middleware (Clamping):** The caching middleware logic has been hardened to strictly enforce the `MinExpirationSeconds` setting at runtime.
  - Previously, `MinExpirationSeconds` was available in the options but not actively enforced by the middleware logic during cache entry creation.
  - Now, if `AbsoluteExpirationSeconds` or `SlidingExpirationSeconds` are configured with values **lower** than the configured `MinExpirationSeconds`, the middleware automatically clamps them to that minimum value.
  - *Note: The default value for `MinExpirationSeconds` is 30 seconds.*
  
### 🐛 Fixes

- **Cache Reliability:** Resolved a stability issue where extremely short cache durations could cause excessive re-merging operations. The system now guarantees the cache duration respects the defined minimum floor.

---

## [1.0.0-alpha.3] - 2026-01-11

### Added

- **Fail-Fast Validation:** Introduced aggressive startup validation to prevent runtime errors. The application will now refuse to start if configuration is invalid:
  - **Source URLs:** All URLs in `Sources` must be valid absolute URIs (must start with `http://` or `https://`).
  - **Gateway URL:** `ApiGatewayBaseUrl` (in OpenAPI options) must be a valid absolute URI.
  - **Paths:** `MergedDocumentPath` must start with `/`.
- **Global Security Options:**
  - Added `GatewaySecurityScheme` to `OpenApiOptions`, enabling configuration of global security schemes directly within the API Gateway context.
  - Added `IgnoreGatewaySecurity` to `OpenApiOptions`, allowing downstream services to retain their own security definitions instead of being overridden by the Gateway's global scheme.
- **Gateway Security Extensions:** Introduced a comprehensive suite of fluent extension methods to configure global security schemes.
  - Methods supported: `UseJwtBearerGatewaySecurity`, `UseApiKeyGatewaySecurity`, `UseBasicAuthGatewaySecurity`, `UseOAuth2ClientCredentialsGatewaySecurity`, `UseOAuth2AuthCodeGatewaySecurity`, and `UseOpenIdConnectGatewaySecurity`.

> **Note 1:** When using Koalesce as a pipeline Middleware, to keep your `appsettings.json` simple, it is recommended to use `OpenApiSecurityExtensions` methods via `.ForOpenAPI(options => ... )` instead of manually configuring the `GatewaySecurityScheme` section.
>
> **Note 2:** When using Koalesce through **Koalesce.OpenAPI.CLI**, you **must** include a `GatewaySecurityScheme` section in the `appsettings.json` if `ApiGatewayBaseUrl` is defined, as the CLI cannot see configurations defined in C# code.

### ⚠️ Breaking Changes

- **Core Options Refactoring (Agnostic Design):** `KoalesceOptions` has been decoupled from OpenAPI-specific terminology to support future formats (AsyncAPI, GraphQL, etc.).
  - **Renamed Properties (Breaking for `appsettings.json`):**
    - `OpenApiSources` → **`Sources`**
    - `MergedOpenApiPath` → **`MergedDocumentPath`**
  - **Renamed Classes:**
    - `OpenApiSourceDefinition` → **`SourceDefinition`**
- **Configuration Moving:**
  - `ApiGatewayBaseUrl` was moved from `KoalesceOptions` **(Core)** to `OpenApiOptions` **(OpenAPI Extension)** to ensure proper separation of concerns.
- **Security Enforcement:** When `ApiGatewayBaseUrl` is set in `OpenApiOptions`, a `GatewaySecurityScheme` is now **required** (unless `IgnoreGatewaySecurity` is true). Startup will fail if the gateway URL is present but no security scheme is defined.

### 🐛 Fixes

- **Dependency Injection Double Binding:** Fixed an issue where `KoalesceOptions` were being bound twice in the DI container when using `AddProvider`, causing configuration conflicts in test scenarios.
- **Middleware Registration:** Fixed a bug where `UseKoalesce()` would silently fail to register the middleware pipeline when using custom provider configurations.
<br/>

### 🔄 Migration Guide (appsettings.json)

Due to the agnostic refactoring of the core options, you must update your configuration files:

**Before (Alpha 2)**
```json
"Koalesce": {
  "OpenApiSources": [ 
    { "Url": "..." } 
  ],
  "MergedOpenApiPath": "/swagger/v1/swagger.json",
}
```

**After (Alpha 3):**
```json
"Koalesce": {
  "Sources": [ 
    { "Url": "..." } 
  ],
  "MergedDocumentPath": "/swagger/v1/swagger.json",
}
```

**Koalesce.OpenAPI.CLI After (Alpha 3): Security configured explicitly in JSON.**
```json
"Koalesce": {
  "Sources": [ 
    { "Url": "..." } 
  ],
  "MergedDocumentPath": "/swagger/v1/swagger.json",
  "ApiGatewayBaseUrl": "https://localhost:5000",
  // GatewaySecurityScheme is REQUIRED here for CLI usage if ApiGatewayBaseUrl is set
  "GatewaySecurityScheme": {
    "Type": "Http",
    "Scheme": "bearer",
    "BearerFormat": "JWT", 
    "Description": "Enter your JWT token"
  }
}
```

---

## [1.0.0-alpha.2] - 2026-01-04

### ⚠️ Breaking Changes
- **Configuration:** Renamed `SourceOpenApiUrls` property to `OpenApiSources`. This change allows adding a `VirtualPrefix` to prevent handle colisions with known identical routes.
> *Note: When using `VirtualPrefix`, the route must be handled in the API Gateway configuration to map correctly to the merged OpenAPI document.*

### Added
- **Multi-targeting:** Added support for **.NET 8.0** (LTS) alongside **.NET 10.0**. The library now targets both frameworks to ensure stability for enterprise projects and performance for modern applications.

### Changed
- **HTTP Client:** The internal `HttpClient` now enforces **HTTP/1.1** protocol.
- **Dev Experience:** The internal HTTP handler now bypasses SSL certificate validation for development environments, fixing issues with self-signed certificates on `https://localhost`.

### Fixed
- **Security Isolation:** Fixed an issue where global security schemes could leak across different APIs in the merged document. Global security requirements are now injected into individual operations during the merge to ensure strict security context isolation.
- **Dependencies:** Removed redundant `Microsoft.Extensions.*` package references in `.csproj` files to eliminate build warnings.
- Ocelot.customers definition now allows route with Id.

---

## [1.0.0-alpha.1] - 2026-01-02

### ⚠️ Breaking Changes
- Upgraded to **.NET 10.0** + NuGet dependencies to compatible versions.
- Removed `--v` argument from **Koalesce.OpenAPI.CLI**. Version is now auto-detected from assembly and displayed with built-in `--version`.

### Added
- '--verbose' argument for detailed logging output. Displaying logs is now optional by default.

---

## [0.1.1-alpha.2] - 2025-04-11

### ⚠️ Breaking Changes
- Upgraded NuGet dependencies to versions fully compatible with **.NET 8.0**.

---

## [0.1.1-alpha.1] - 2025-04-08

### ⚠️ Breaking Changes
- Dropped support for `.NET 6.0` and `.NET 7.0`.
- The library now targets `.NET 8.0` exclusively for modern compatibility and to leverage latest runtime features.

---

## [0.1.0-alpha] - 2025-03-16

### Added
- Alpha release of **Koalesce**.
- OpenAPI merging from multiple sources.
- Support for **API Gateway** integration.
- **Caching options** for merged definitions.
- Initial middleware for serving merged OpenAPI documents.
