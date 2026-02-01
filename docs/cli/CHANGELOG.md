# Changelog [Koalesce.CLI]

All notable changes to **Koalesce.CLI** will be documented in this file.

> This changelog follows [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) and [Semantic Versioning](https://semver.org/).

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
