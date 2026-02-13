# 🔀 Conflict Resolution

When merging multiple OpenAPI specs, conflicts are inevitable. Koalesce handles **path conflicts** and **schema conflicts** with explicit strategies — no silent data loss.

---

## 🟰 Identical Paths

When two services define the same path (e.g., `/api/health`), there's no perfect solution. Koalesce gives you three strategies — each with clear trade-offs:

### Strategy 1️⃣: VirtualPrefix (Preserve All Paths) ⭐ Recommended
```json
{
  "Sources": [
    { "Url": "https://inventory-api/swagger.json", "VirtualPrefix": "/inventory" },
    { "Url": "https://catalog-api/swagger.json", "VirtualPrefix": "/catalog" }
  ]
}
```

**Result:**
```
Original paths:          Merged spec:
/api/health       →      /inventory/api/health
/api/health       →      /catalog/api/health
```

**✅ Pros:**
- All endpoints preserved.
- No data loss.
- Explicit service boundaries in merged spec.

**⚠️ Cons:**
- **Requires Gateway URL rewrite** (Ocelot, YARP, Kong, etc.).
- Gateway must strip prefix before routing to actual service.
- More configuration needed.

**Use when:** You have a Gateway and want complete API coverage.


### Strategy 2️⃣: First Source Wins (Default)

```json
{
  "Sources": [
    { "Url": "https://inventory-api/swagger.json" },
    { "Url": "https://catalog-api/swagger.json" }
  ]
}
```

**Result:**
```
Source            Path          Merged spec
Inventory API  →  /api/health → ✅ Included
Catalog API    →  /api/health → ⚠️ Skipped (warning logged)
```

**✅ Pros:**
- Zero Gateway configuration.
- Predictable behavior.
- Works out-of-the-box.

**⚠️ Cons:**
- **Later sources lose conflicting paths**.
- Not suitable if you need all endpoints.
- Health checks, status endpoints often duplicated.

**Use when:** You're okay with losing duplicate paths, or paths are naturally unique


### Strategy 3️⃣: Fail-Fast (Strict Mode)
```json
{
  "Sources": [
    { "Url": "https://inventory-api/swagger.json" },
    { "Url": "https://catalog-api/swagger.json" }
  ],
  "SkipIdenticalPaths": false
}
```

**Result:**
```
❌ KoalesceIdenticalPathFoundException
   Duplicate path detected: /api/health
   Sources: inventory-api, catalog-api
```

**✅ Pros:**
- Forces you to resolve conflicts explicitly.
- Perfect for CI/CD validation.
- No silent data loss.

**⚠️ Cons:**
- Requires upfront path design coordination
- Fails on common paths like `/health`, `/ready`

**Use when:** You want strict contract enforcement or are validating service designs

---

## 🟰 Identical Schemas

**Automatic Resolution:** When multiple APIs define schemas with identical names (e.g., `Product`), Koalesce automatically renames them using the (customizable) pattern `{Prefix}{SchemaName}`.

**Conflict Behavior:**

| Scenario | Result |
|---|---|
| Both sources have `VirtualPrefix` | **Both** schemas are renamed (e.g., `InventoryProduct`, `CatalogProduct`.) |
| Only one source has `VirtualPrefix` | Only the prefixed source's schema is renamed |
| Neither source has `VirtualPrefix` | First schema keeps original name. Second uses **Sanitized API Title** as prefix. |

> 💡 **Note:** When falling back to the API Title, Koalesce sanitizes the string (PascalCase, alphanumeric only) to ensure valid C# identifiers. For example, `"Sales API v2"` becomes `SalesApiV2`.

**Prefix Priority:**

1. **VirtualPrefix** (if configured): `/inventory` → `InventoryProduct`
2. **API Name** (sanitized): `Koalesce.Samples.InventoryAPI` → `KoalesceSamplesInventoryAPIProduct`

---

## 🤔 Which strategy is the best for you?

Conflicts are an **architectural decision**, not a technical problem. Koalesce makes the trade-offs explicit and lets you choose the strategy that fits your architecture.

**Recommendation:**
  - Use `VirtualPrefix` with a Gateway for production.
  - Use `First Wins` for simple scenarios or development.
  - Use `Fail-Fast` in CI/CD to enforce path uniqueness.
