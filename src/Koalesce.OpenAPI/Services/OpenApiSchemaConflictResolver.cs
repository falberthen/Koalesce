namespace Koalesce.OpenAPI.Services;

/// <summary>
/// Tracks the origin of a schema for conflict resolution
/// </summary>
internal record SchemaOrigin(string ApiName, string? VirtualPrefix);

internal static class OpenApiSchemaConflictResolver
{
	/// <summary>
	/// Detects schema collisions and renames source schemas to avoid overwriting
	/// When both source and existing schema have VirtualPrefix, BOTH are renamed
	/// Priority: VirtualPrefix > Last Segment of Title > Full Title
	/// </summary>
	public static void ResolveSchemaConflicts(
		ILogger logger,
		OpenApiDocument sourceDocument,
		OpenApiDocument targetDocument,
		string apiName,
		string? virtualPrefix,
		string? schemaConflictPattern = null,
		Dictionary<string, SchemaOrigin>? schemaOrigins = null)
	{
		if (sourceDocument.Components?.Schemas is null || targetDocument.Components?.Schemas is null)
			return;

		schemaConflictPattern ??= CoreConstants.DefaultSchemaConflictPattern;
		schemaOrigins ??= new Dictionary<string, SchemaOrigin>();

		var sourceRenames = new Dictionary<string, string>();
		var targetRenames = new Dictionary<string, string>();
		var keysToRemove = new List<string>();

		// Determine the scope prefix for the source
		string sourceScopePrefix = DetermineSchemaScopePrefix(apiName, virtualPrefix);

		// Identify collisions
		foreach (var (key, schema) in sourceDocument.Components.Schemas)
		{
			// Only rename if the key ALREADY exists in the target (Collision)
			if (targetDocument.Components.Schemas.ContainsKey(key))
			{
				// Check if the existing schema in target came from a source with VirtualPrefix
				bool existingHasVirtualPrefix = schemaOrigins.TryGetValue(key, out var existingOrigin)
					&& !string.IsNullOrWhiteSpace(existingOrigin.VirtualPrefix);

				bool currentHasVirtualPrefix = !string.IsNullOrWhiteSpace(virtualPrefix);

				// If BOTH have VirtualPrefix, rename BOTH schemas
				if (existingHasVirtualPrefix && currentHasVirtualPrefix)
				{
					// Rename the existing schema in target
					string existingPrefix = DetermineSchemaScopePrefix(existingOrigin!.ApiName, existingOrigin.VirtualPrefix);
					string existingNewKey = ApplySchemaConflictPattern(schemaConflictPattern, key, existingPrefix);

					if (!targetRenames.ContainsKey(key) && existingNewKey != key)
					{
						targetRenames[key] = existingNewKey;
						logger.LogInformation("Schema collision detected for '{Key}'. Renaming existing schema to '{NewKey}' (from '{ApiName}').",
							key, existingNewKey, existingOrigin.ApiName);
					}
				}

				// Generate a scoped name for the source schema
				string newKey = ApplySchemaConflictPattern(schemaConflictPattern, key, sourceScopePrefix);

				// Edge Case: If the scoped name ALSO exists, fallback to fully qualified name
				if (targetDocument.Components.Schemas.ContainsKey(newKey) || targetRenames.ContainsValue(newKey))
				{
					string fullPrefix = CleanName(apiName);
					newKey = ApplySchemaConflictPattern(schemaConflictPattern, key, fullPrefix);
				}

				logger.LogInformation("Schema collision detected for '{Key}' in API '{ApiName}'. Renaming to '{NewKey}'.",
					key, apiName, newKey);

				sourceRenames[key] = newKey;
				keysToRemove.Add(key);
			}
		}

		// Apply target renames first (existing schemas that need to be prefixed)
		if (targetRenames.Count > 0)
		{
			foreach (var (oldKey, newKey) in targetRenames)
			{
				if (targetDocument.Components.Schemas.TryGetValue(oldKey, out var schema))
				{
					targetDocument.Components.Schemas.Remove(oldKey);
					targetDocument.Components.Schemas[newKey] = schema;

					// Update the origin tracking
					if (schemaOrigins.TryGetValue(oldKey, out var origin))
					{
						schemaOrigins.Remove(oldKey);
						schemaOrigins[newKey] = origin;
					}
				}
			}

			// Rewrite references in the target document
			var targetRewriter = new SchemaReferenceRewriter(targetRenames);
			var targetWalker = new OpenApiWalker(targetRewriter);
			targetWalker.Walk(targetDocument);
		}

		if (sourceRenames.Count == 0)
			return;

		// Update the Source Document definitions
		foreach (var key in keysToRemove)
		{
			var schema = sourceDocument.Components.Schemas[key];
			sourceDocument.Components.Schemas.Remove(key);
			sourceDocument.Components.Schemas.Add(sourceRenames[key], schema);
		}

		// Walk the entire source document to update all References ($ref)
		var rewriter = new SchemaReferenceRewriter(sourceRenames);
		var walker = new OpenApiWalker(rewriter);
		walker.Walk(sourceDocument);

		// Track the origin of the new schemas
		foreach (var (oldKey, newKey) in sourceRenames)
		{
			schemaOrigins[newKey] = new SchemaOrigin(apiName, virtualPrefix);
		}
	}

	/// <summary>
	/// Determines the best prefix for scoping schemas based on configuration.
	/// </summary>
	private static string DetermineSchemaScopePrefix(string apiName, string? virtualPrefix)
	{
		// Best Strategy: Virtual Prefix configured by the Developer
		// e.g., "/inventory" -> "Inventory"
		if (!string.IsNullOrWhiteSpace(virtualPrefix))
			return ToPascalCase(CleanName(virtualPrefix));

		// Fallback Strategy: Sanitized API Title
		// Removes spaces, dots, and special characters to ensure a valid identifier
		// e.g., "Inventory API" -> "InventoryAPI"
		// e.g., "Koalesce.Samples.Inventory" -> "KoalesceSamplesInventory"
		return ToPascalCase(CleanName(apiName));
	}

	/// <summary>
	/// Converts the first character of a string to uppercase (PascalCase).
	/// </summary>
	private static string ToPascalCase(string input)
	{
		if (string.IsNullOrEmpty(input))
			return string.Empty;

		return char.ToUpperInvariant(input[0]) + input[1..];
	}

	/// <summary>
	/// Applies the schema conflict pattern to generate a new schema name.
	/// </summary>
	private static string ApplySchemaConflictPattern(string pattern, string schemaName, string prefix)
	{
		return pattern
			.Replace(CoreConstants.PrefixPlaceholder, prefix)
			.Replace(CoreConstants.SchemaNamePlaceholder, schemaName);
	}

	/// <summary>
	/// Removes all non-alphanumeric characters from the specified string
	/// </summary>
	private static readonly Regex _nonAlphaNumericRegex = new(@"[^a-zA-Z0-9_]", RegexOptions.Compiled);
	private static string CleanName(string input)
	{
		if (string.IsNullOrEmpty(input))
			return string.Empty;

		return _nonAlphaNumericRegex.Replace(input, "");
	}
}