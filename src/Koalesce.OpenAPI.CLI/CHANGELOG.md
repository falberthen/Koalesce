> This changelog follows the [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) format and adheres to [Semantic Versioning](https://semver.org/).

# Changelog

## [1.0.0-alpha.1] - 2026-01-02

### ⚠️ Breaking Changes
- 🚀 Upgraded to **.NET 10.0** + NuGet dependencies to compatible versions.
- ❌ Removed '--v' argument; version is now auto-detected from assembly and displayed with built-in '--version'.

### Added
- ⚡ '--verbose' argument for detailed logging output. Displaying logs is now optional by default.

---

## [0.1.1-alpha.1] - 2025-04-10

### ⚠️ Breaking Changes
- 🚀 Upgraded NuGet dependencies to versions fully compatible with **.NET 8.0**.

### Fixed
- ⚡Improved error handling when writing output files.

### Changed
- ⚡Enhanced CLI UI/UX with ANSI styling and clearer messages.

---

## [0.1.0-alpha] - 2025-04-10

### Added
- 🚀 Initial alpha release of **Koalesce.OpenAPI.CLI**.
- ⚡ Support for merging OpenAPI definitions via Koalesce and writing them to disk.
