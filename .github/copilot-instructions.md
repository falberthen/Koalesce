# Koalesce

## Project Overview

**Koalesce** is an open-source, lightweight library that merges multiple OpenAPI definitions into a single unified definition.


## Tech Stack

- **Backend:** .NET 8.0 / .NET 10.0 (multi-target).
- **OpenAPI:** Microsoft.OpenApi for parsing and serialization.
- **DI:** Microsoft.Extensions.DependencyInjection.
- **Configuration:** Microsoft.Extensions.Configuration with Options pattern.
- **Testing:** xUnit, WebApplicationFactory for integration tests.
- **CI:** GitHub Actions.

## Project Structure

- `src/Koalesce/` - OpenAPI provider implementation.
- `src/Koalesce.Core/` - Core abstractions and base options.
- `src/Koalesce.CLI/` - Command-line tool.
- `tests/Koalesce.Core.Tests/` - Unit tests for Koalesce.Core.
- `tests/Koalesce.Tests/` - Unit and integration tests for Koalesce.
- `tests/Koalesce.CLI.Tests/` - Tests for CLI tool.
- `samples/Koalesce/` - Working examples (Swagger UI, Ocelot gateway).

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
- A `GlobalUsings.cs` file should be placed at the root of the provider project for common using directives.
  

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

