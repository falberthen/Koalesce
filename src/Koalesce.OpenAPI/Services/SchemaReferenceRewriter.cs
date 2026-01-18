namespace Koalesce.OpenAPI.Services;

/// <summary>
/// Internal visitor to rewrite Schema reference Ids in the source document
/// Used when renaming schemas to avoid conflicts
/// </summary>
internal class SchemaReferenceRewriter : OpenApiVisitorBase
{
	private readonly IReadOnlyDictionary<string, string> _renames;

	public SchemaReferenceRewriter(IReadOnlyDictionary<string, string> renames)
	{
		_renames = renames;
	}

	public override void Visit(OpenApiSchema schema)
	{
		if (schema?.Reference is null)
			return;

		// Only rewrite component schema references
		if (schema.Reference.Type != ReferenceType.Schema)
			return;

		if (!_renames.TryGetValue(schema.Reference.Id, out var newId))
			return;

		// OpenApiReference.ReferenceV3 is read-only (computed)
		// Replacing the entire reference object with a new Id
		var oldRef = schema.Reference;

		schema.Reference = new OpenApiReference
		{
			Type = ReferenceType.Schema,
			Id = newId,

			// Preserve external references if they exist (rare but possible)
			ExternalResource = oldRef.ExternalResource
		};

		// Ensure the schema remains a reference-only schema (no inline leftovers)
		schema.UnresolvedReference = false;
	}
}
