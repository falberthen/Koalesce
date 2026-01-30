# Release Process

This document describes how to release new versions of Koalesce packages to NuGet.

## Package Structure

Koalesce has three packages with **independent versioning**:

```
Koalesce.Core              → Independent version (core-v1.0.0-alpha.13)
Koalesce.OpenAPI           → Independent version (openapi-v1.0.0-alpha.13)
Koalesce.OpenAPI.CLI       → Independent version (openapi-cli-v1.0.0-alpha.12.4)
```

Each package can be released independently, or all three can be released together with the same version.

## Version Management

All versions are centrally managed in the `Versions.props` file at the repository root:

```xml
<Project>
  <PropertyGroup>
    <!-- Each package has independent versioning -->
    <KoalesceCoreVersion>1.0.0-alpha.12</KoalesceCoreVersion>
    <KoalesceOpenAPIVersion>1.0.0-alpha.12</KoalesceOpenAPIVersion>
    <KoalesceCLIVersion>1.0.0-alpha.12.3</KoalesceCLIVersion>
  </PropertyGroup>
</Project>
```

## Release Scenarios

There are four ways to release packages:

### Scenario 1: Publishing Only Koalesce.Core

Use this when you've made changes only to the Core library.

**Steps:**

1. **Update the version** in `Versions.props`:
   ```xml
   <KoalesceCoreVersion>1.0.0-alpha.13</KoalesceCoreVersion>
   ```

2. **Update CHANGELOG.md** with the changes in this release

3. **Commit and push** the changes:
   ```bash
   git add Versions.props docs/CHANGELOG.md
   git commit -m "Bump Core version to 1.0.0-alpha.13"
   git push origin master
   ```

4. **Create a GitHub Release**:
   - Go to https://github.com/falberthen/Koalesce/releases/new
   - Create a new tag with format: `core-v1.0.0-alpha.13` (must start with `core-v`)
   - Set the release title: `Core v1.0.0-alpha.13`
   - Add release notes describing the Core changes
   - Click "Publish release"

5. **Automated Publishing**:
   - The GitHub Actions workflow will automatically:
     - Validate that the tag version matches `KoalesceCoreVersion` in `Versions.props`
     - Build the solution
     - Run all tests
     - Pack `Koalesce.Core`
     - Publish to NuGet.org
     - Upload package as artifact

6. **Verify the release**:
   - Check the GitHub Actions workflow: https://github.com/falberthen/Koalesce/actions
   - Verify package on NuGet.org: https://www.nuget.org/packages/Koalesce.Core

### Scenario 2: Publishing Only Koalesce.OpenAPI

Use this when you've made changes only to the OpenAPI provider library.

**Steps:**

1. **Update the version** in `Versions.props`:
   ```xml
   <KoalesceOpenAPIVersion>1.0.0-alpha.13</KoalesceOpenAPIVersion>
   ```

2. **Update CHANGELOG.md** with the changes in this release

3. **Commit and push** the changes:
   ```bash
   git add Versions.props docs/CHANGELOG.md
   git commit -m "Bump OpenAPI version to 1.0.0-alpha.13"
   git push origin master
   ```

4. **Create a GitHub Release**:
   - Go to https://github.com/falberthen/Koalesce/releases/new
   - Create a new tag with format: `openapi-v1.0.0-alpha.13` (must start with `openapi-v`)
   - Set the release title: `OpenAPI v1.0.0-alpha.13`
   - Add release notes describing the OpenAPI changes
   - Click "Publish release"

5. **Automated Publishing**:
   - The GitHub Actions workflow will automatically:
     - Validate that the tag version matches `KoalesceOpenAPIVersion` in `Versions.props`
     - Build the solution
     - Run all tests
     - Pack `Koalesce.OpenAPI`
     - Publish to NuGet.org
     - Upload package as artifact

6. **Verify the release**:
   - Check the GitHub Actions workflow: https://github.com/falberthen/Koalesce/actions
   - Verify package on NuGet.org: https://www.nuget.org/packages/Koalesce.OpenAPI

### Scenario 3: Publishing Only Koalesce.OpenAPI.CLI

Use this when you've made changes only to the CLI tool.

**Steps:**

1. **Update the CLI version** in `Versions.props`:
   ```xml
   <KoalesceCLIVersion>1.0.0-alpha.12.4</KoalesceCLIVersion>
   ```

2. **Update the CLI CHANGELOG** at `docs/cli/CHANGELOG.md`

3. **Commit and push** the changes:
   ```bash
   git add Versions.props docs/cli/CHANGELOG.md
   git commit -m "Bump CLI version to 1.0.0-alpha.12.4"
   git push origin master
   ```

4. **Create a GitHub Release**:
   - Go to https://github.com/falberthen/Koalesce/releases/new
   - Create a new tag with format: `openapi-cli-v1.0.0-alpha.12.4` (must start with `openapi-cli-v`)
   - Set the release title: `CLI v1.0.0-alpha.12.4`
   - Add release notes describing the CLI changes
   - Click "Publish release"

5. **Automated Publishing**:
   - The GitHub Actions workflow will automatically:
     - Validate that the tag version matches `KoalesceCLIVersion` in `Versions.props`
     - Build the solution
     - Pack `Koalesce.OpenAPI.CLI`
     - Publish to NuGet.org
     - Upload package as artifact

6. **Verify the release**:
   - Check the GitHub Actions workflow: https://github.com/falberthen/Koalesce/actions
   - Verify package on NuGet.org: https://www.nuget.org/packages/Koalesce.OpenAPI.CLI

### Scenario 4: Publishing All Packages Together

Use this when you've made changes across multiple packages and want to release them all with the same version number.

**Steps:**

1. **Update all versions** in `Versions.props` to the same version:
   ```xml
   <KoalesceCoreVersion>1.0.0</KoalesceCoreVersion>
   <KoalesceOpenAPIVersion>1.0.0</KoalesceOpenAPIVersion>
   <KoalesceCLIVersion>1.0.0</KoalesceCLIVersion>
   ```

2. **Update CHANGELOGs** with the changes in this release

3. **Commit and push** the changes:
   ```bash
   git add Versions.props docs/CHANGELOG.md docs/cli/CHANGELOG.md
   git commit -m "Bump all packages to 1.0.0"
   git push origin master
   ```

4. **Create a GitHub Release**:
   - Go to https://github.com/falberthen/Koalesce/releases/new
   - Create a new tag with format: `v1.0.0` (must start with `v` only, no prefix)
   - Set the release title: `v1.0.0`
   - Add release notes describing changes across all packages
   - Click "Publish release"

5. **Automated Publishing**:
   - The GitHub Actions workflow will automatically:
     - Validate that all three versions in `Versions.props` match the tag version
     - Build the solution
     - Run all tests
     - Pack all three packages (`Koalesce.Core`, `Koalesce.OpenAPI`, `Koalesce.OpenAPI.CLI`)
     - Publish all packages to NuGet.org
     - Upload packages as artifacts

6. **Verify the release**:
   - Check the GitHub Actions workflow: https://github.com/falberthen/Koalesce/actions
   - Verify all packages on NuGet.org:
     - https://www.nuget.org/packages/Koalesce.Core
     - https://www.nuget.org/packages/Koalesce.OpenAPI
     - https://www.nuget.org/packages/Koalesce.OpenAPI.CLI

## When to Use Each Scenario

| Scenario | Use When | Tag Format | Example |
|----------|----------|------------|---------|
| **Core Only** | Bug fixes or features in Core library only | \`core-v*\` | \`core-v1.0.1\` |
| **OpenAPI Only** | Bug fixes or features in OpenAPI provider only | \`openapi-v*\` | \`openapi-v1.0.1\` |
| **CLI Only** | Bug fixes or features in CLI tool only | \`openapi-cli-v*\` | \`openapi-cli-v1.0.1\` |
| **All Together** | Major releases, coordinated updates, or breaking changes | \`v*\` | \`v1.0.0\` |

## Initial Setup (One-Time Configuration)

Before the automated workflows can publish to NuGet, you need to configure the \`NUGET_API_KEY\` secret.

## Quick Reference

| Package | Version Property | Tag Format | Workflow File |
|---------|-----------------|------------|---------------|
| Koalesce.Core | \`KoalesceCoreVersion\` | \`core-v*\` | \`nuget-publish-core.yml\` |
| Koalesce.OpenAPI | \`KoalesceOpenAPIVersion\` | \`openapi-v*\` | \`nuget-publish-openapi.yml\` |
| Koalesce.OpenAPI.CLI | \`KoalesceCLIVersion\` | \`openapi-cli-v*\` | \`nuget-publish-openapi-cli.yml\` |
| All Packages | All three (must match) | \`v*\` | \`nuget-publish-all.yml\` |
