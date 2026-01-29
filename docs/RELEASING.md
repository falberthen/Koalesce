# Release Process

This document describes how to release new versions of Koalesce packages to NuGet.

## Package Structure

Koalesce has two types of packages with independent versioning:

```
Koalesce.Core         ─┐
                       ├── Same version (libraries released together)
Koalesce.OpenAPI      ─┘
                             ↑
Koalesce.OpenAPI.CLI  ────── Independent version (CLI tool, consumes the libraries)
```

## Version Management

All versions are centrally managed in the `Versions.props` file at the repository root:

```xml
<Project>
  <PropertyGroup>
    <!-- Library version (Core + OpenAPI always together) -->
    <KoalesceLibVersion>1.0.0-alpha.12</KoalesceLibVersion>
    
    <!-- CLI version (independent, can evolve separately) -->
    <KoalesceCLIVersion>1.0.0-alpha.12.3</KoalesceCLIVersion>
  </PropertyGroup>
</Project>
```

## Releasing Libraries (Koalesce.Core and Koalesce.OpenAPI)

The Core and OpenAPI libraries are always released together with the same version number.

### Steps:

1. **Update the version** in `Versions.props`:
   ```xml
   <KoalesceLibVersion>1.0.0-alpha.13</KoalesceLibVersion>
   ```

2. **Update CHANGELOG.md** with the changes in this release

3. **Commit and push** the changes:
   ```bash
   git add Versions.props docs/CHANGELOG.md
   git commit -m "Bump library version to 1.0.0-alpha.13"
   git push origin master
   ```

4. **Create a GitHub Release**:
   - Go to https://github.com/falberthen/Koalesce/releases/new
   - Create a new tag with format: `v1.0.0-alpha.13` (must start with `v`)
   - Set the release title: `v1.0.0-alpha.13`
   - Add release notes describing the changes
   - Click "Publish release"

5. **Automated Publishing**:
   - The GitHub Actions workflow will automatically:
     - Build the solution
     - Run all tests
     - Pack both `Koalesce.Core` and `Koalesce.OpenAPI`
     - Publish to NuGet.org
     - Upload packages as artifacts

6. **Verify the release**:
   - Check the GitHub Actions workflow: https://github.com/falberthen/Koalesce/actions
   - Verify packages on NuGet.org:
     - https://www.nuget.org/packages/Koalesce.Core
     - https://www.nuget.org/packages/Koalesce.OpenAPI

## Releasing CLI (Koalesce.OpenAPI.CLI)

The CLI tool can be released independently from the libraries.

### Steps:

1. **Update the CLI version** in `Versions.props`:
   ```xml
   <KoalesceCLIVersion>1.0.0-alpha.12.4</KoalesceCLIVersion>
   ```
   
   Note: If the CLI needs to depend on a newer library version, also update:
   ```xml
   <KoalesceLibVersion>1.0.0-alpha.13</KoalesceLibVersion>
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
   - Create a new tag with format: `cli-v1.0.0-alpha.12.4` (must start with `cli-v`)
   - Set the release title: `CLI v1.0.0-alpha.12.4`
   - Add release notes describing the CLI changes
   - Click "Publish release"

5. **Automated Publishing**:
   - The GitHub Actions workflow will automatically:
     - Build the CLI project
     - Pack `Koalesce.OpenAPI.CLI`
     - Publish to NuGet.org
     - Upload package as artifact

6. **Verify the release**:
   - Check the GitHub Actions workflow: https://github.com/falberthen/Koalesce/actions
   - Verify package on NuGet.org: https://www.nuget.org/packages/Koalesce.OpenAPI.CLI

## Initial Setup (One-Time Configuration)

Before the automated workflows can publish to NuGet, you need to configure the `NUGET_API_KEY` secret:

### Creating a NuGet API Key:

1. **Sign in to NuGet.org** at https://www.nuget.org/

2. **Create an API Key**:
   - Go to https://www.nuget.org/account/apikeys
   - Click "Create"
   - Configure the key:
     - **Key Name**: `Koalesce GitHub Actions`
     - **Expiration**: Choose appropriate duration (e.g., 365 days)
     - **Scopes**: Select "Push" and optionally "Push new packages and package versions"
     - **Glob Pattern**: `Koalesce.*` (to limit scope to Koalesce packages)
   - Click "Create"
   - **Copy the API key** (you won't be able to see it again!)

3. **Add the secret to GitHub**:
   - Go to https://github.com/falberthen/Koalesce/settings/secrets/actions
   - Click "New repository secret"
   - **Name**: `NUGET_API_KEY`
   - **Value**: Paste the API key from NuGet.org
   - Click "Add secret"

## Version Numbering Guidelines

### Semantic Versioning

Follow [Semantic Versioning 2.0.0](https://semver.org/):
- **MAJOR** version: Incompatible API changes
- **MINOR** version: Backwards-compatible functionality additions
- **PATCH** version: Backwards-compatible bug fixes

### Pre-release Versions

Use pre-release identifiers for unstable versions:
- **Alpha**: `1.0.0-alpha.1` (early development, unstable)
- **Beta**: `1.0.0-beta.1` (feature complete, testing)
- **RC**: `1.0.0-rc.1` (release candidate, final testing)

### CLI Versioning Strategy

The CLI version can have an additional segment to indicate CLI-specific updates:
- Libraries: `1.0.0-alpha.12`
- CLI: `1.0.0-alpha.12.3` (third CLI release using library version 1.0.0-alpha.12)

This allows the CLI to be updated independently for bug fixes or improvements without releasing new library versions.

## Troubleshooting

### Workflow Fails on Publish

If the workflow fails with "package already exists":
- The `--skip-duplicate` flag will prevent errors on retries
- Check if the version in `Versions.props` was updated
- Verify you're not accidentally re-releasing the same version

### Package Dependencies

The CLI depends on the library packages from NuGet.org by default. When releasing:
1. Release the libraries first if there are changes
2. Wait for the packages to be available on NuGet.org (usually a few minutes)
3. Then release the CLI

For local development and CI, the `USE_PROJECT_REFS` constant switches to project references.

### Testing Before Release

To test packaging locally without publishing:

```bash
# Build and pack locally
dotnet build --configuration Release --property:DefineConstants=USE_PROJECT_REFS
dotnet pack src/Koalesce.Core/Koalesce.Core.csproj --configuration Release --output ./nupkgs
dotnet pack src/Koalesce.OpenAPI/Koalesce.OpenAPI.csproj --configuration Release --output ./nupkgs
dotnet pack src/Koalesce.OpenAPI.CLI/Koalesce.OpenAPI.CLI.csproj --configuration Release --output ./nupkgs

# Inspect the packages
ls -lh ./nupkgs/
```

## Best Practices

1. **Always update CHANGELOGs** before releasing
2. **Test thoroughly** before creating a release
3. **Use descriptive release notes** in GitHub releases
4. **Follow semantic versioning** strictly
5. **Keep libraries in sync**: Always release Core and OpenAPI together
6. **Document breaking changes** prominently in release notes
7. **Rotate API keys** periodically for security
8. **Monitor the Actions workflow** after each release

## Release Checklist

### For Library Releases:
- [ ] Update `KoalesceLibVersion` in `Versions.props`
- [ ] Update `docs/CHANGELOG.md` with changes
- [ ] Run tests locally: `dotnet test --configuration Release`
- [ ] Commit and push changes
- [ ] Create GitHub release with tag `v{version}`
- [ ] Verify workflow success in GitHub Actions
- [ ] Verify packages on NuGet.org

### For CLI Releases:
- [ ] Update `KoalesceCLIVersion` in `Versions.props`
- [ ] Update `KoalesceLibVersion` if needed
- [ ] Update `docs/cli/CHANGELOG.md` with changes
- [ ] Test CLI locally
- [ ] Commit and push changes
- [ ] Create GitHub release with tag `cli-v{version}`
- [ ] Verify workflow success in GitHub Actions
- [ ] Verify package on NuGet.org
- [ ] Test installing the tool: `dotnet tool install -g Koalesce.OpenAPI.CLI --version {version}`
