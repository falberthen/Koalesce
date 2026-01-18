# Changelog

All notable changes to this project will be documented in this file.
> This changelog follows the [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) format and adheres to [Semantic Versioning](https://semver.org/).

---

## [1.0.0-alpha.6] - 2026-01-18

### Changed

- **Core Update:** Upgraded `Koalesce.OpenAPI` core library to version **1.0.0-alpha.6**.
- **Security Configuration:** `OpenApiSecurityScheme` is now fully optional in `appsettings.json`. When omitted, downstream API security configurations are preserved as-is in the merged document.

### ⚠️ Breaking Changes

- **Removed `IgnoreGatewaySecurity` property:** This property is no longer needed. Simply omit `OpenApiSecurityScheme` from your `appsettings.json` to preserve downstream security.
- **Renamed `GatewaySecurityScheme` property:** Now called `OpenApiSecurityScheme` to align with the OpenAPI specification terminology.

### 🔄 Migration Guide

**Before (Alpha 5):**

```json
{
  "Koalesce": {
    "ApiGatewayBaseUrl": "https://localhost:5000",
    "GatewaySecurityScheme": { ... }  // Required
  }
}
```

**After (Alpha 6):**

```json
{
  "Koalesce": {
    "ApiGatewayBaseUrl": "https://localhost:5000",
    "OpenApiSecurityScheme": { ... }  // Optional - omit to preserve downstream security
  }
}
```

---

## [1.0.0-alpha.5] - 2026-01-15

### Added

- **NuGet Metadata:** Added official project icon.

---

## [1.0.0-alpha.4] - 2026-01-13

### Changed

- **Core Update:** Upgraded `Koalesce` core libraries to version **1.0.0-alpha.4**.

---

## [1.0.0-alpha.3] - 2026-01-11

### ⚠️ Breaking Changes
- **Security Enforcement:** When `ApiGatewayBaseUrl` is set in `OpenApiOptions`, a `OpenApiSecurityScheme` is now **required** (unless `IgnoreGatewaySecurity` is true). Startup will fail if the gateway URL is present but no security scheme is defined.
> **Note:** When using Koalesce through **Koalesce.OpenAPI.CLI**, you **must** include a `OpenApiSecurityScheme` section in the `appsettings.json` if `ApiGatewayBaseUrl` is defined, as the CLI cannot see configurations defined in C# code.

---

## [1.0.0-alpha.2] - 2026-01-02

### Changed
- Improved the CLI output format. `VirtualPrefix` is now highlighted in **Magenta** to visually distinguish the virtual path from the physical source URL.

---

## [1.0.0-alpha.1] - 2026-01-02

### ⚠️ Breaking Changes
- Removed '--v' argument; version is now auto-detected from assembly and displayed with built-in '--version'.

### Added
- '--verbose' argument for detailed logging output. Displaying logs is now optional by default.

---

## [0.1.1-alpha.1] - 2025-04-10

### Changed
- Upgraded NuGet dependencies to versions fully compatible with **.NET 8.0**.
- Enhanced CLI UI/UX with ANSI styling and clearer messages.

### 🐛 Fixes
- Improved error handling when writing output files.

---

## [0.1.0-alpha] - 2025-04-10

### Added
- Initial alpha release of **Koalesce.OpenAPI.CLI**.
- Support for merging OpenAPI definitions via Koalesce and writing merged definitions to disk.
