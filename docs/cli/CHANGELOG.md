# Changelog [Koalesce.CLI]

All notable changes to **Koalesce.CLI** will be documented in this file.

> This changelog follows [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) and [Semantic Versioning](https://semver.org/).

---

## [1.0.0-beta.4] - 2026-02-05

### Changed

- Updated `Koalesce` dependency to `1.0.0-beta.4`.
- **Microsoft.OpenApi upgrade**: Updated from `2.0.0` to `3.3.1`.
- **Central package management**: Migrated to `Directory.Packages.props` for centralized NuGet package versions.

### Improved

- **UX Improvements**:
   - When not able to load sources, in a non-strict configuration mode (FailOnServiceLoadError = false), UI will display `[Not loaded]` after the Url or FilePath.
   - **Internal errors and Exceptions** are only displayed if parameter `--verbose` is used. Otherwise, they're wrapped with friendly messages.

---

## [1.0.0-beta.3] - 2026-02-02

### Added

- **SSL bypass flag**: Added `--insecure` (`-k`, `-i`) option to skip SSL certificate validation when fetching API specs from sources with self-signed certificates.
- **Option shortcuts**: Added `-o` for `--output` and `-c` for `--config`.
- **Banner on empty args**: Shows the Koalesce banner when running `koalesce` without arguments.

### Changed

- Updated `Koalesce` dependency to `1.0.0-beta.3`.
- Output path using the same font color used for loaded sources.
- Removed default config file name "appsettings.json" from error message when argument is missing.

### Improved

- **Categorized error messages**: More descriptive error messages with exit codes (1=config, 2=network, 3=file, 99=unexpected).

---

## [1.0.0-beta.2] - 2026-02-01

### Changed

- Updated `Koalesce` dependency to `1.0.0-beta.2`.

---

## [1.0.0-beta.1] - 2026-02-01

First **beta release** of `Koalesce.CLI` - a command-line tool for merging OpenAPI specifications using `Koalesce`.

### Features

- **Merge Command**: Merge multiple OpenAPI specs into a single file.
- **Configuration-Based**: Uses a `.json` file for source definitions.
- **Multiple Output Formats**: JSON and YAML output support.
- **Verbose Mode**: `--verbose` flag for detailed operation logging.

### Installation

```bash
dotnet tool install --global Koalesce.CLI --prerelease
```

### Usage

```bash
koalesce --config ./config/appsettings.json --output ./merged-specs/apigateway.yaml --verbose
```

### Targets

- .NET 8.0 (LTS)
- .NET 10.0 (LTS)

### Migration from Alpha

If upgrading from `Koalesce.OpenAPI.CLI` alpha versions:

- Package renamed from `Koalesce.OpenAPI.CLI (now Deprecated)` to `Koalesce.CLI`.
- Command syntax unchanged.
- Configuration files remain compatible.
