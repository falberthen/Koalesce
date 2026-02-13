# Changelog [Koalesce]

All notable changes to **Koalesce** will be documented in this file.

> This changelog follows [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) and [Semantic Versioning](https://semver.org/).

---

## [1.0.0-beta.6] - 2026-02-13

### Added
- **PrefixTagsWith** option for per-source tag prefixing: Allows prefixing all tags from a source with a custom label (e.g., "Payments" → "Payments - Users"), improving Swagger UI
grouping when merging multiple APIs.

### Fixed

- **Schemas were left orpham in the merged definition when paths were excluded by ExcludePaths**: Added CollectReferencedSchemas to walk all path references transitively and remove unreferenced schemas after the merge.

### Changed

#### ⚠️ Breaking Change
- Moved **Title** into a new **Info** settings section, of type [OpenApiInfo](https://learn.microsoft.com/en-us/dotnet/api/microsoft.openapi.openapiinfo). This object fully complies with Microsoft.OpenApi.
  ```json
  {
    "Koalesce": {    
      "Info": {
        "Title": "My Koalesced API",
        "Description": "Unified API aggregating multiple services"
        ...
      },
  ```

---

## [1.0.0-beta.5] - 2026-02-07

### Fixed

- **Leading wildcard patterns in ExcludePaths**: Fixed `*/segment/*` patterns not matching paths correctly. Patterns like `*/admin/*` now properly match paths containing `/admin/` anywhere (e.g., `/api/admin/users`, `/v1/admin/settings`).

---

## [1.0.0-beta.4] - 2026-02-05

### Added

- **Input version validation**: Validates OpenAPI versions from source documents before parsing. Unsupported versions are rejected with clear error messages.
- **Extended version support**: Added support for `3.0.2`, `3.0.3`, `3.1.1`, and `3.2.0`.

### Changed

- **Microsoft.OpenApi upgrade**: Updated from `2.0.0` to `3.3.1`, enabling support for OpenAPI 3.1.x and 3.2.x specifications.
- **Centralized version constants**: `SupportedOpenApiVersions` moved to `KoalesceConstants` for consistency between input and output validation.
- **Central package management**: Migrated to `Directory.Packages.props` for centralized NuGet package versions.

---

## [1.0.0-beta.3] - 2026-02-02

### Added

- **HttpClient customization**: Added `configureHttpClient` parameter to `AddKoalesce()` allowing consumers to customize the HttpClient used for fetching API specs (SSL/TLS, authentication handlers, retry policies, etc.).
- **Flexible wildcard patterns**: `ExcludePaths` now supports wildcards (`*`) anywhere in the pattern (e.g., `/api/*`, `/*/health`, `/api/*/details`).

### Changed

#### ⚠️ Breaking Change

- **Removed built-in SSL bypass**: Use configureHttpClient parameter instead.
-  Koalesce no longer bypasses SSL certificate validation by default. Consumers who need this behavior can configure it via the new `configureHttpClient` parameter.

  - **Migration:** If you were relying on the implicit SSL bypass for self-signed certificates:

    ```csharp
    services.AddKoalesce(configuration, configureHttpClient: builder =>
        builder.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (msg, cert, chain, errors) => true
        }));
    ```

### Improved

- **Improved error responses**: Middleware now returns structured JSON error responses instead of exposing raw exception messages.

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

#### ⚠️ Breaking Changes

- `SchemaConflictPattern` default value is now `{Prefix}{SchemaName}`.
- `MergedDocumentPath` is now `MergedEndpoint`.
