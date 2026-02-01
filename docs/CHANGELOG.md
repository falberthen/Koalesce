# Changelog

All notable changes to **Koalesce** will be documented in this file.

> This changelog follows [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) and [Semantic Versioning](https://semver.org/).

---

## [1.0.0-beta.2] - 2026-02-01

### Fixed

- **URL-based sources now prefer servers from source document**: When fetching OpenAPI specs via URL (e.g., internal Docker hostnames), Koalesce now uses the `servers` declared in the source document instead of extracting the base URL from the fetch URL. This fixes Swagger UI "Failed to fetch" errors when APIs declare public URLs different from their internal fetch URLs. Falls back to previous behavior if no servers are declared.
- **Nullable warning in `KoalesceMiddleware`**: Fixed CS8618 warning for `_mergedEndpoint` field.

---

## [1.0.0-beta.1] - 2026-02-01

First **beta release** of `Koalesce` - a library for merging multiple OpenAPI specifications into a single unified definition.

### Features

- **OpenAPI 3.1.0 Support**: Full compatibility with `OpenAPI 3.0` and `3.1` specifications via `Microsoft.OpenApi 2.0`.
- **Multiple Source Types**
  - HTTP/HTTPS URLs for live API endpoints.
  - Local file paths (JSON/YAML) for offline specifications.
- **Smart Schema Conflict Resolution**: Automatic renaming of conflicting schemas using configurable patterns.
- **Virtual Prefix Routing**: Namespace paths per source to avoid route collisions (e.g., `/inventory/products`, `/orders/products`).
- **Path Exclusion**: Filter out specific paths per source using exact matches or wildcards.
- **Caching**: Built-in response caching with configurable expiration.
- **ASP.NET Core Integration**: Middleware for serving merged specs at a configurable endpoint.
- **Resilient Loading**: Configurable behavior for unreachable sources (skip or fail-fast).
- **Security Preservation**: Maintains downstream API security schemes in merged output.

### Targets

- .NET 8.0 (LTS)
- .NET 10.0 (LTS)

### Migration from Alpha

If upgrading from `Koalesce.OpenAPI (now Deprecated)` alpha versions:

- Package renamed from `Koalesce.OpenAPI` to `Koalesce`.
- `Koalesce.Core` is now bundled internally (no separate package needed).
- Configuration structure unchanged - `appsettings.json` files remain compatible.
  - #### ⚠️ Breaking Changes
    - `SchemaConflictPattern` default value is now `{Prefix}{SchemaName}`.
    - `MergedDocumentPath` is now `MergedEndpoint`.


