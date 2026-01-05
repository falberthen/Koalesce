> This changelog follows the [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) format and adheres to [Semantic Versioning](https://semver.org/).

# Changelog

## [1.0.0-alpha.2] - 2026-01-04

### ⚠️ Breaking Changes
- **Configuration:** Renamed `SourceOpenApiUrls` property to `OpenApiSources`. This change allows adding a `VirtualPrefix` to prevent handle colisions with known identical routes.
> *Note: When using `VirtualPrefix`, the route must be handled in the API Gateway configuration to map correctly to the merged OpenAPI document.*

### 🚀 Added
- **Multi-targeting:** Added support for **.NET 8.0** (LTS) alongside **.NET 10.0**. The library now targets both frameworks to ensure stability for enterprise projects and performance for modern applications.

### ⚡ Changed
- **HTTP Client:** The internal `HttpClient` now enforces **HTTP/1.1** protocol.
- **Dev Experience:** The internal HTTP handler now bypasses SSL certificate validation for development environments, fixing issues with self-signed certificates on `https://localhost`.
- **CLI UI:** Improved the CLI output format. `VirtualPrefix` is now highlighted in **Magenta** to visually distinguish the virtual path from the physical source URL.

### 🐛 Fixed
- **Security Isolation:** Fixed an issue where global security schemes could leak across different APIs in the merged document. Global security requirements are now injected into individual operations during the merge to ensure strict security context isolation.
- **Dependencies:** Removed redundant `Microsoft.Extensions.*` package references in `.csproj` files to eliminate build warnings.
- Ocelot.customers definition now allows route with Id.

---

## [1.0.0-alpha.1] - 2026-01-02

### ⚠️ Breaking Changes
- Upgraded to **.NET 10.0** + NuGet dependencies to compatible versions.
- Removed '--v' argument; version is now auto-detected from assembly and displayed with built-in '--version'.

### 🚀 Added
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

### 🚀 Added
- Alpha release of **Koalesce**.
- OpenAPI merging from multiple sources.
- Support for **API Gateway** integration.
- **Caching options** for merged definitions.
- Initial middleware for serving merged OpenAPI documents.
