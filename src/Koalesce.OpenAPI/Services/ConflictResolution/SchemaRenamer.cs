namespace Koalesce.OpenAPI.Services.ConflictResolution;

/// <summary>
/// Service responsible for renaming schemas in OpenAPI documents to resolve naming conflicts.
/// </summary>
internal class SchemaRenamer
{
	private readonly ILogger<SchemaRenamer> _logger;

	public SchemaRenamer(ILogger<SchemaRenamer> logger)
	{
		_logger = logger;
	}

	/// <summary>
	/// Applies renaming logic to the Target document (renaming existing schemas).
	/// </summary>
	public void ApplyRenamesToTarget(
		OpenApiDocument targetDocument,
		Dictionary<string, string> targetRenames,
		Dictionary<string, SchemaOrigin> schemaOrigins)
	{
		if (targetRenames.Count == 0) return;

		foreach (var (oldKey, newKey) in targetRenames)
		{
			if (targetDocument.Components.Schemas.TryGetValue(oldKey, out var schema))
			{
				targetDocument.Components.Schemas.Remove(oldKey);
				targetDocument.Components.Schemas[newKey] = schema;

				// Update origin tracking
				if (schemaOrigins.TryGetValue(oldKey, out var origin))
				{
					schemaOrigins.Remove(oldKey);
					schemaOrigins[newKey] = origin;
				}

				// Changed to Debug to avoid log noise on large merges
				_logger.LogDebug("Target Schema Renamed: '{Old}' -> '{New}'", oldKey, newKey);
			}
		}

		// Rewrite internal references within the target document
		var rewriter = new SchemaReferenceRewriter(targetRenames);
		var walker = new OpenApiWalker(rewriter);
		walker.Walk(targetDocument);
	}

	/// <summary>
	/// Applies renaming logic to the Source document (renaming incoming schemas before merge).
	/// </summary>
	public void ApplyRenamesToSource(
		OpenApiDocument sourceDocument,
		Dictionary<string, string> sourceRenames)
	{
		if (sourceRenames.Count == 0) return;

		foreach (var (oldKey, newKey) in sourceRenames)
		{
			// Assuming source schema still holds the old key
			if (sourceDocument.Components.Schemas.TryGetValue(oldKey, out var schema))
			{
				sourceDocument.Components.Schemas.Remove(oldKey);
				sourceDocument.Components.Schemas.Add(newKey, schema);
			}
		}

		// Rewrite internal references within the source document
		var rewriter = new SchemaReferenceRewriter(sourceRenames);
		var walker = new OpenApiWalker(rewriter);
		walker.Walk(sourceDocument);
	}
}