# Changelog

All notable changes to this project will be documented in this file.
> This changelog follows the [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) format and adheres to [Semantic Versioning](https://semver.org/).

---

## [1.0.0-alpha.12.1] - 2026-01-28

### Fixed

- **Installation Error:** Fixed a critical issue where `dotnet tool install` would fail with "Settings file 'DotnetToolSettings.xml' was not found".

### Changed

- **Packaging Strategy:** The CLI package is now **self-contained**. It bundles the `Koalesce.Core` and `Koalesce.OpenAPI` dependencies (v1.0.0-alpha.12) internally.

---

## [1.0.0-alpha.12] - 2026-01-28

### Changed

- **Core Update:** Upgraded `Koalesce` core libraries to version **1.0.0-alpha.12**.

### ⚠️ Breaking Changes (via Core libraries)

- **Configuration Validation:** The CLI will now **fail to start** if the `appsettings.json` contains duplicate URLs or FilePaths in the `Sources` list. Previously, duplicates might have been processed redundantly.
- **OpenAPI 3.1.0 Output:** Due to the upgrade to `Microsoft.OpenApi` 2.0, the generated output document may now utilize OpenAPI 3.1.0 features. Downstream tools that only support OpenAPI 2.0 or 3.0 may need updates.

---

## [1.0.0-alpha.11] - 2026-01-25

### Changed

- **Core Update:** Upgraded `Koalesce` core libraries to version **1.0.0-alpha.11**.

### ⚠️ Breaking Changes (via Core libraries)

- **Security Logic:** Removed optional support for mixed security schemes. The system now always preserves downstream security schemes by design.

---

## [1.0.0-alpha.10] - 2026-01-22

### Changed

- **Core Update:** Upgraded `Koalesce` core libraries to version **1.0.0-alpha.10**.

### ⚠️ Breaking Changes (via Core libraries)

- **Schema Naming Strategy (Output Change):** The logic for resolving conflicting schema names is now deterministic. Sources with `VirtualPrefix` will **always** prefix their schemas (e.g., `Inventory_Product`), changing the output structure.

---

## [1.1.0-alpha.9] - 2026-01-21

### Changed

- **Core Update:** Upgraded `Koalesce` core libraries to version **1.1.0-alpha.9**.

---

## [1.1.0-alpha.8] - 2026-01-19

### Changed

- **Core Update:** Upgraded `Koalesce` core libraries to version **1.1.0-alpha.8**.

---

## [1.1.0-alpha.7] - 2026-01-19

### Changed

- **Core Update:** Upgraded `Koalesce` core libraries to version **1.1.0-alpha.7**.

---

## [1.0.0-alpha.6] - 2026-01-18

### Changed

- **Core Update:** Upgraded `Koalesce` core libraries to version **1.0.0-alpha.6**.

### ⚠️ Breaking Changes (via Core libraries)

- **Configuration Rename:** `GatewaySecurityScheme` property in `appsettings.json` is now named **`OpenApiSecurityScheme`**.
- **Removed Property:** `IgnoreGatewaySecurity` is no longer supported.

---

## [1.0.0-alpha.5] - 2026-01-15

### Added

- **NuGet Metadata:** Added official project icon.

### Changed

- **Core Update:** Upgraded `Koalesce` core libraries to version **1.0.0-alpha.5**.

---

## [1.0.0-alpha.4] - 2026-01-13

### Changed

- **Core Update:** Upgraded `Koalesce` core libraries to version **1.0.0-alpha.4**.

---

## [1.0.0-alpha.3] - 2026-01-11

### Changed

- **Core Update:** Upgraded `Koalesce` core libraries to version **1.0.0-alpha.3**.

### ⚠️ Breaking Changes (via Core libraries)

- **Configuration Renames:** `OpenApiSources` → **`Sources`**; `MergedOpenApiPath` → **`MergedDocumentPath`**.
- **Security Requirement:** If `ApiGatewayBaseUrl` is defined, `OpenApiSecurityScheme` is now required.

---

## [1.0.0-alpha.2] - 2026-01-02

### Changed

- **CLI Output:** `VirtualPrefix` is now highlighted in **Magenta**.
- **Core Update:** Upgraded `Koalesce` core libraries to version **1.0.0-alpha.2**.

### ⚠️ Breaking Changes (via Core libraries)

- **Configuration Rename:** `SourceOpenApiUrls` → **`OpenApiSources`**.

---

## [1.0.0-alpha.1] - 2026-01-02

### ⚠️ Breaking Changes
- Removed `--v` argument (use `--version`).

### Added
- `--verbose` argument.

### Changed
- **Core Update:** Upgraded `Koalesce` core libraries to version **1.0.0-alpha.1**.

---

## [0.1.1-alpha.1] - 2025-04-10

### Changed
- Upgraded NuGet dependencies to versions fully compatible with **.NET 8.0**.
- Enhanced CLI UI/UX.

### Fixed
- Improved error handling when writing output files.

---

## [0.1.0-alpha] - 2025-04-10

### Added
- Initial alpha release of **Koalesce.OpenAPI.CLI**.