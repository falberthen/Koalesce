> This changelog follows the [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) format and adheres to [Semantic Versioning](https://semver.org/).

# Changelog

## [1.0.0-alpha.1] - 2026-01-02

### ⚠️ Breaking Changes
- 🚀 Upgraded to **.NET 10.0** + NuGet dependencies to compatible versions.
- ❌ Removed '--v' argument; version is now auto-detected from assembly and displayed with built-in '--version'.

### Added
- ⚡ '--verbose' argument for detailed logging output. Displaying logs is now optional by default.

---

## [0.1.1-alpha.2] - 2025-04-11

### ⚠️ Breaking Changes
- 🚀 Upgraded NuGet dependencies to versions fully compatible with **.NET 8.0**.

---

## [0.1.1-alpha.1] - 2025-04-08

### ⚠️ Breaking Changes
- ❌ Dropped support for `.NET 6.0` and `.NET 7.0`.
- ⚡ The library now targets `.NET 8.0` exclusively for modern compatibility and to leverage latest runtime features.

---

## [0.1.0-alpha] - 2025-03-16

### Added
- 🚀 Alpha release of **Koalesce**.
- ⚡ OpenAPI merging from multiple sources.
- ⚡ Support for **API Gateway** integration.
- ⚡ **Caching options** for merged definitions.
- ⚡ Initial middleware for serving merged OpenAPI documents.
