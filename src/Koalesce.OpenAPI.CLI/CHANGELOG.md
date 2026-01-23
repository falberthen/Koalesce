# Changelog

All notable changes to this project will be documented in this file.
> This changelog follows the [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) format and adheres to [Semantic Versioning](https://semver.org/).

---

## [1.0.0-alpha.10] - 2026-01-22

### Added

- **Fail-Fast Configuration:** Introduced `FailOnServiceLoadError` option (default: `false`).
  - When set to `true`, Koalesce will abort the startup process if **any** source API fails to load (network error, timeout, invalid JSON).
  - When set to `false` (default), Koalesce continues to operate resiliently, skipping unreachable sources.

### Changed

- **Internal Architecture:**
  - Implemented `DefaultConflictResolutionStrategy` for clearer conflict resolution.
  - Refactored `OpenApiDocumentMerger` to adhere to Single Responsibility Principle (SRP).
  - Extracted `OpenApiDefinitionLoader` for robust I/O handling.
  - Extracted `SchemaConflictCoordinator` and `SchemaRenamer` for isolated conflict logic.

### ⚠️ Breaking Changes

- **Schema Conflict Resolution Strategy:**
  - The logic for resolving schema name conflicts is now deterministic based on `VirtualPrefix`.
  - **With Prefix:** Sources defining a `VirtualPrefix` will have their schemas scoped to that prefix (e.g., `Inventory_Product`).
  - **No Prefix:** Sources without a prefix act as the "root" domain. If a conflict occurs with another non-prefixed source, the incoming source falls back to using its Sanitized API Title.
  - *Impact:* Generated clients (Kiota/NSwag) will require refactoring as class names will change to match the new scoping rules.

---

## [1.1.0-alpha.9] - 2026-01-21

### Fixed

- **Schema Conflict Resolution with VirtualPrefix:** When **both** conflicting sources have `VirtualPrefix` configured, **both** schemas are now renamed. Previously, only the second schema was renamed, making the output order-dependent.
  - Before: `Product` (first wins), `Inventory_Product`
  - After: `Products_Product`, `Inventory_Product`

### Added

- **Duplicate VirtualPrefix Validation:** Fail-fast validation now prevents configuration errors when multiple sources share the same `VirtualPrefix`. This prevents path collisions and asymmetric schema naming at runtime.

### Changed

- **Core Update:** Upgraded `Koalesce.OpenAPI` core library to version **1.1.0-alpha.9**.

---

## [1.1.0-alpha.8] - 2026-01-19

### Added

- **Exclude Paths from Merge:** New `ExcludePaths` option per source in `appsettings.json` allows excluding specific paths from the merged document.
  - Supports exact matches (e.g., `"/api/internal"`)
  - Supports wildcard patterns (e.g., `"/api/admin/*"`)
  - **Fail-fast validation:** Paths must start with `/`, cannot be empty, and wildcards are only supported at the end (`/*`)

**Example configuration:**

```json
{
  "Koalesce": {
    "Sources": [
      {
        "Url": "https://api.example.com/swagger.json",
        "ExcludePaths": [
          "/api/internal",
          "/api/admin/*"
        ]
      }
    ]
  }
}
```

### Changed

- **Core Update:** Upgraded `Koalesce.OpenAPI` core library to version **1.1.0-alpha.8**.
- **Moved `SchemaConflictPattern` to Core Options:** `SchemaConflictPattern` has been moved from `OpenApiOptions` to `KoalesceOptions` (Core). No changes required in `appsettings.json` configuration.

---

## [1.1.0-alpha.7] - 2026-01-19

### Added

- **Customizable Schema Conflict Pattern:** New `SchemaConflictPattern` option in `appsettings.json` allows customizing how schema name conflicts are resolved.
  - Default: `"{Prefix}_{SchemaName}"` (e.g., `Inventory_Product`)
  - Available placeholders: `{Prefix}`, `{SchemaName}`

**Example configuration:**

```json
{
  "Koalesce": {
    "SchemaConflictPattern": "{SchemaName}_{Prefix}"
  }
}
```

### Changed

- **Core Update:** Upgraded `Koalesce.OpenAPI` core library to version **1.1.0-alpha.7**.

---

## [1.0.0-alpha.6] - 2026-01-18

### Changed

- **Core Update:** Upgraded `Koalesce.OpenAPI` core library to version **1.0.0-alpha.7**.
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
