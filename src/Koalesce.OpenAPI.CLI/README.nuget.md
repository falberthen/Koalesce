# 🐨 Koalesce.OpenAPI CLI

**Koalesce.OpenAPI.CLI** is the official .NET global tool designed to extend the capabilities of [Koalesce](https://github.com/falberthen/Koalesce) beyond middleware integration, enabling developers to merge multiple OpenAPI definitions into a single, unified specification file on disk.

---

## 🛠️ Usage

#### **Arguments**:

- `--config` specifies the path to your `appsettings.json` configuration file with Koalesce settings.
- `--output` defines the path where the merged OpenAPI specification file will be saved.
- `--version` displays the current version of the Koalesce.OpenAPI.CLI tool.
- `--verbose` enables detailed logging output for debugging purposes.


#### **Basic Command Structure**

```bash
koalesce --config <path-to-appsettings.json> --output <path-to-output-spec>
```

### **Example**

```bash
koalesce --config ./config/appsettings.json --output ./merged-specs/apigateway.yaml
```

---

#### 📝 License

Koalesce is licensed under the [**MIT License**](https://github.com/falberthen/Koalesce/blob/master/LICENSE).

#### ❤️ Contributing

Contributions are welcome! Feel free to open issues and submit PRs.

#### 📧 Contact

For support or inquiries, reach out via **GitHub Issues**.

#### 📜 Koalesce.OpenAPI.CLI Changelog

See the full changelog [here](https://github.com/falberthen/Koalesce/tree/master/src/Koalesce.OpenAPI.CLI/CHANGELOG.md).