# Koalesce CLI Tool

![Koalesce](https://raw.githubusercontent.com/falberthen/Koalesce/master/img/cli_small.png)

**Koalesce.CLI** is a standalone command-line tool that uses [Koalesce](https://github.com/falberthen/Koalesce#readme) to merge multiple OpenAPI definitions into a single unified API specification, and save it to a file on disk.

![.NET](https://img.shields.io/badge/.NET-8-512BD4?logo=dotnet) ![.NET](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet) [![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

⭐ **If you find Koalesce useful, please consider giving it a star on [GitHub](https://github.com/falberthen/Koalesce)!**

---

## 🧩 The Problem

Building microservices or modular APIs? You're probably dealing with:

- 🔀 Frontend teams juggling **multiple Swagger UIs** across services.
- 📚 Scattered API documentation with no **unified view for consumers**.
- 🔍 No single place to explore, test, or share your full API surface.
- 🛠️ Client SDK generation from **scattered, disconnected specs**.

---

## 💡The Solution

![Koalesce](https://raw.githubusercontent.com/falberthen/Koalesce/master/img/koalesce_diagram.png)

---

## 📦 Quick Start

### 1️⃣ Install

```bash
# Koalesce as a CLI standalone tool
dotnet tool install --global Koalesce.CLI --prerelease
```

### 2️⃣ Configure

```json
// your .json file
{
  "Koalesce": {
    "OpenApiVersion": "3.0.1",
    "Info": {
      "Title": "My 🐨Koalesced API",
      "Description": "Unified API aggregating multiple services"
    },
    "Sources": [
      {
        "Url": "https://localhost:8002/swagger/v1/swagger.json",
        "VirtualPrefix": "/catalog",
        "ExcludePaths": ["/internal/*", "*/admin/*"],
        "PrefixTagsWith": "Catalog"
      },
      {
        "Url": "https://localhost:8003/swagger/v1/swagger.json",
        "VirtualPrefix": "/inventory",
        "PrefixTagsWith": "Inventory"
      }
    ]
  }
}
```

### 3️⃣ Run it

```bash
  koalesce -c .\appsettings.json -o .\Output\apigateway.yaml
```

![Koalesce CLI Screenshot](https://raw.githubusercontent.com/falberthen/Koalesce/master/img/Screenshot_CLI_Sample.png)
💡 The CLI merges OpenAPI definitions directly into a file on disk without requiring a host application.

---

## CLI arguments

| Option       | Shortcut   | Required |                                                  |
| ------------ | ---------- | -------- | ----------------------------------------------------------- |
| `--config`   | `-c`       | 🔺Yes   | Path to your configuration `.json` file.                    |
| `--output`   | `-o`       | 🔺Yes   | Path for the merged OpenAPI spec file.                      |
| `--insecure` | `-k`, `-i` | No       | Skip SSL certificate validation (for self-signed certs).    |
| `--verbose`  |            | No       | Enable detailed logging.                                    |
| `--version`  |            | No       | Display current version.                                    |

---

## 📐 How It Works

**1. Fetch APIs** 
- Read from URLs (`https://api.com/swagger.json`) or local files (`./path/localspec.yaml`). 
- Supports OpenAPI 2.0, 3.0.x, 3.1.x, 3.2.x in JSON and YAML formats.

**2. Resolve Conflicts** 
- Path conflicts are handled by your choice: *VirtualPrefix*, *First Wins*, or *Fail-Fast*. 
- Schema name collisions are auto-renamed based on configuration (e.g., `Inventory.Product` → `InventoryProduct`).

**3. Output**  
- A single unified OpenAPI spec (JSON or YAML), targeting any version, ready for Swagger UI, Scalar, Kiota, or NSwag.

---

## 📜 Important Links

- 📖Configuration and advanced usage
  - [Koalesce Configuration Reference](https://github.com/falberthen/Koalesce/blob/master/docs/CONFIGURATION.md)
  - [Koalesce CLI Arguments Reference](https://github.com/falberthen/Koalesce/blob/master/docs/cli/CLI-ARGUMENTS.md)
  - [Conflict Resolution Strategies](https://github.com/falberthen/Koalesce/blob/master/docs/CONFLICT-RESOLUTION.md)
- 📖 Changelogs
  - [Koalesce Changelog](https://github.com/falberthen/Koalesce/blob/master/docs/CHANGELOG.md)
  - [Koalesce.CLI Changelog](https://github.com/falberthen/Koalesce/tree/master/docs/cli/CHANGELOG.md)

---

## 📧 Support & Contributing

- **Issues**: Report bugs or request features via [GitHub Issues](https://github.com/falberthen/Koalesce/issues).
- **Contributing**: Contributions are welcome! Please read [CONTRIBUTING.md](https://github.com/falberthen/Koalesce/tree/master/docs/CONTRIBUTING.md) before submitting PRs.
- **Sample Projects**: Check out [Koalesce.Samples](https://github.com/falberthen/Koalesce/tree/master/samples) for a complete implementation.

---

## 📝 License

Koalesce is licensed under the [**MIT License**](https://github.com/falberthen/Koalesce/blob/master/LICENSE).

---

> ⚠️ **Migration:** The packages `Koalesce.OpenAPI` and `Koalesce.OpenAPI.CLI` are now deprecated. Please migrate to `Koalesce` and `Koalesce.CLI`.
