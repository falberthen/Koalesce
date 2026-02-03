namespace Koalesce.Services.ConflictResolution;

/// <summary>
/// Service for rewriting schema references in OpenAPI documents.
/// </summary>
internal interface ISchemaReferenceRewriter
{
	/// <summary>
	/// Rewrites all schema references in the document according to the rename map.
	/// </summary>
	/// <param name="document">The OpenAPI document to process.</param>
	/// <param name="renames">Dictionary mapping old schema names to new names.</param>
	void RewriteReferences(OpenApiDocument document, IReadOnlyDictionary<string, string> renames);
}
