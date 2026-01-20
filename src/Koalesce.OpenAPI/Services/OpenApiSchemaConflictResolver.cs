namespace Koalesce.OpenAPI.Services;

internal static class OpenApiSchemaConflictResolver
{
	/// <summary>
	/// Detects schema collisions and renames source schemas to avoid overwriting
	/// Priority: VirtualPrefix > Last Segment of Title > Full Title
	/// </summary>
	public static void ResolveSchemaConflicts(
		ILogger logger,
		OpenApiDocument sourceDocument,
		OpenApiDocument targetDocument,
		string apiName,
		string? virtualPrefix,
		string schemaConflictPattern = "{Prefix}_{SchemaName}")
	{
		if (sourceDocument.Components?.Schemas is null || targetDocument.Components?.Schemas is null)
			return;

		var renames = new Dictionary<string, string>();
		var keysToRemove = new List<string>();

		// Determine the scope prefix
		string scopePrefix = DetermineSchemaScopePrefix(apiName, virtualPrefix);

		// Identify collisions
		foreach (var (key, schema) in sourceDocument.Components.Schemas)
		{
			// Only rename if the key ALREADY exists in the target (Collision)
			if (targetDocument.Components.Schemas.ContainsKey(key))
			{
				// Generate a scoped name using the configured pattern
				string newKey = ApplySchemaConflictPattern(schemaConflictPattern, key, scopePrefix);

				// Edge Case: If the scoped name ALSO exists, fallback to fully qualified name
				if (targetDocument.Components.Schemas.ContainsKey(newKey))
				{
					string fullPrefix = CleanName(apiName);
					newKey = ApplySchemaConflictPattern(schemaConflictPattern, key, fullPrefix);
				}

				logger.LogInformation("Schema collision detected for '{Key}' in API '{ApiName}'. Renaming to '{NewKey}'.",
					key, apiName, newKey);

				renames[key] = newKey;
				keysToRemove.Add(key);
			}
		}

		if (renames.Count == 0)
			return;

		// Update the Source Document definitions
		foreach (var key in keysToRemove)
		{
			var schema = sourceDocument.Components.Schemas[key];
			sourceDocument.Components.Schemas.Remove(key);
			sourceDocument.Components.Schemas.Add(renames[key], schema);
		}

		// Walk the entire source document to update all References ($ref)
		// using the OpenApiWalker to safely traverse paths, operations, parameters, and other schemas.
		var rewriter = new SchemaReferenceRewriter(renames);
		var walker = new OpenApiWalker(rewriter);
		walker.Walk(sourceDocument);
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
			.Replace("{Prefix}", prefix)
			.Replace("{SchemaName}", schemaName);
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