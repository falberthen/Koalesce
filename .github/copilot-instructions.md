# Koalesce

## Project Overview

**Koalesce** is an open-source, lightweight and extensible library designed to merge multiple API definitions into a unified document.

## Tech Stack

- **Backend:** .NET 8.0 / .NET 10.0 (multi-target).
- **OpenAPI:** Microsoft.OpenApi for parsing and serialization.
- **DI:** Microsoft.Extensions.DependencyInjection.
- **Configuration:** Microsoft.Extensions.Configuration with Options pattern.
- **Testing:** xUnit, WebApplicationFactory for integration tests.
- **CI:** GitHub Actions.

## Project Structure

- `src/Koalesce.Core/` - Core abstractions and base options (provider-agnostic).
- `src/Koalesce.OpenAPI/` - OpenAPI provider implementation.
- `src/Koalesce.OpenAPI.CLI/` - Command-line tool.
- `tests/Koalesce.Core.Tests/` - Unit tests for Koalesce.Core.
- `tests/Koalesce.OpenAPI.Tests/` - Unit and integration tests for OpenAPI provider.
- `tests/Koalesce.OpenAPI.CLI.Tests/` - Tests for CLI tool.
- `samples/Koalesce.OpenAPI/` - Working examples for OpenAPI provider (Swagger UI, Ocelot gateway).

## Provider Development Guidelines

- Code for each provider must be in its own project under `src/`, named `Koalesce.[ProviderName]`.
- Options classes must be placed in `Options/`, inheriting from `KoalesceOptions` and overriding `Validate()` for provider-specific validation.
- A main provider's class (e.g. `KoalesceOpenApiProvider`) must be placed at the root of the provider project, inheriting from `KoalesceProviderBase<TOptions, TMergeResult>`.
- A provider must extend `IKoalesceBuilder` via an extension method in `Extensions/` for DI registration with a method named `For[ProviderName]`.
- Supporting services (e.g. parsers, mergers) must be placed in `Services/` (with subfolders for organization) and registered in the extension method.
- Provider-specific constants must be placed in `Constants/`.
- A `GlobalUsings.cs` file should be placed at the root of the provider project for common using directives.
- Tests for the provider must be placed in `tests/Koalesce.[ProviderName].Tests/`.
- Sample usage for the provider must be placed in `samples/Koalesce.[ProviderName]/` with projects named `Koalesce.[ProviderName].Samples.*` and included in `Koalesce.Samples.sln`.

## General Coding Guidelines

- Adhere to .NET coding conventions: https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions
- Use async/await for all I/O operations.
- Adhere to SOLID principles.
- Use `TryAddSingleton` for stateless services, never `AddTransient`.
- Error message constants go in `Constants/` folder.
- Avoid instance-level state in singleton services; use local variables for per-request data.
- Use collection expressions (`[]`) instead of `new List<T>()`.
- Prefer `.Count == 0` over `.Any()` for emptiness checks.
- All public APIs must have XML documentation.

## Code Examples

### Registering a Provider

```csharp
// In Extensions/KoalesceFor[ProviderName]BuilderExtensions.cs
public static IKoalesceBuilder For[ProviderName](
    this IKoalesceBuilder builder,
    Action<Koalesce[ProviderName]Options>? configureOptions = null)
{
    var services = builder.Services;

    // Register services
    services.TryAddSingleton<IDocumentMerger<TDocument>, MyDocumentMerger>();
    services.TryAddSingleton<IMergedDocumentSerializer<TDocument>, MySerializer>();
    services.TryAddSingleton<IKoalesceProvider, Koalesce[ProviderName]Provider>();

    // Apply optional code-based configuration
    if (configureOptions != null)
        services.PostConfigure(configureOptions);

    return builder.AddProvider<Koalesce[ProviderName]Provider, Koalesce[ProviderName]Options>();
}
```

### Provider Options with Validation

```csharp
// In Options/Koalesce[ProviderName]Options.cs
public class Koalesce[ProviderName]Options : KoalesceOptions
{
    public string? CustomSetting { get; set; }

    public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // Always call base validation first
        foreach (var result in base.Validate(validationContext))
            yield return result;

        // Provider-specific validation
        if (string.IsNullOrWhiteSpace(CustomSetting))
            yield return new ValidationResult("CustomSetting is required.", [nameof(CustomSetting)]);
    }
}
```

## Anti-Patterns (Avoid)

```csharp
// ❌ Don't use AddTransient for stateless services
services.AddTransient<IMyService, MyService>();

// ✅ Use TryAddSingleton instead
services.TryAddSingleton<IMyService, MyService>();

// ❌ Don't use new List<T>()
var items = new List<string>();

// ✅ Use collection expressions
var items = new List<string> { "a", "b" }; // or List<string> items = [];

// ❌ Don't use .Any() for emptiness checks
if (!items.Any()) { }

// ✅ Use .Count == 0
if (items.Count == 0) { }

// ❌ Don't store request-scoped data in singleton fields
private string _currentRequest; // Bad in singleton!

// ✅ Use local variables or pass as parameters
public void Process(string requestData) { }
```
