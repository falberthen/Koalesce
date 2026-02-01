namespace Koalesce.Services.ConflictResolution;

/// <summary>
/// Helper methods for name generation, sanitization, and uniqueness checks.
/// </summary>
internal static class SchemaNameUtils
{
	/// <summary>
	/// Determines the best prefix for scoping schemas based on configuration.
	/// </summary>
	public static string DetermineSchemaScopePrefix(string apiName, string? virtualPrefix)
	{
		// Virtual Prefix configured by the Developer
		if (!string.IsNullOrWhiteSpace(virtualPrefix))
			return virtualPrefix.CleanName().ToPascalCase();

		// Sanitized API Title
		return apiName.CleanName().ToPascalCase();
	}

	/// <summary>
	/// Applies the schema conflict pattern to generate a new schema name.
	/// </summary>
	public static string ApplySchemaConflictPattern(string pattern, string schemaName, string prefix)
	{
		return pattern
			.Replace(CoreConstants.PrefixPlaceholder, prefix)
			.Replace(CoreConstants.SchemaNamePlaceholder, schemaName);
	}

	/// <summary>
	/// Ensures the generated key is unique. If a collision persists even after applying the pattern,
	/// it appends a numeric suffix to guarantee uniqueness.
	/// </summary>
	public static string EnsureUniqueKey(
		string newKey,
		string originalKey,
		string apiName,
		string schemaConflictPattern,
		OpenApiDocument targetDocument,
		Dictionary<string, string> currentBatchRenames)
	{
		string candidateKey = newKey;
		int collisionCount = 0;

		// Loop until we find a key that doesn't exist in the Target or the Current Batch
		while (targetDocument.Components?.Schemas?.ContainsKey(candidateKey) == true ||
			   currentBatchRenames.ContainsValue(candidateKey))
		{
			// First attempt: Apply the standard conflict pattern (e.g., ApiName + SchemaName)
			string fullPrefix = apiName.CleanName();
			string standardResolvedKey = ApplySchemaConflictPattern(schemaConflictPattern, originalKey, fullPrefix);

			if (collisionCount == 0 && candidateKey != standardResolvedKey)
			{
				// Try the standard resolution first
				candidateKey = standardResolvedKey;
			}
			else
			{
				// Secondary collision: The standard resolved name ALSO exists.
				// Fallback: Append a numeric suffix to ensure uniqueness (e.g., ApiNameSchemaName_1)
				collisionCount++;
				candidateKey = $"{standardResolvedKey}_{collisionCount}";
			}
		}

		return candidateKey;
	}
}