namespace Koalesce.Services.ConflictResolution;

/// <summary>
/// Service for walking and manipulating schema references in OpenAPI documents.
/// </summary>
internal interface ISchemaReferenceWalker
{
	/// <summary>
	/// Rewrites all schema references in the document according to the rename map.
	/// </summary>
	/// <param name="document">The OpenAPI document to process.</param>
	/// <param name="renames">Dictionary mapping old schema names to new names.</param>
	void RewriteReferences(OpenApiDocument document, IReadOnlyDictionary<string, string> renames);

	/// <summary>
	/// Collects the names of all schemas referenced by paths/operations in the document,
	/// including transitive references from nested schemas.
	/// </summary>
	/// <param name="document">The OpenAPI document to scan.</param>
	/// <returns>A set of schema names that are actively referenced.</returns>
	HashSet<string> CollectReferencedSchemas(OpenApiDocument document);
}
