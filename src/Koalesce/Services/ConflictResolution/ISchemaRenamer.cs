namespace Koalesce.Services.ConflictResolution;

/// <summary>
/// Interface for renaming schemas in OpenAPI documents to resolve naming conflicts.
/// </summary>
internal interface ISchemaRenamer
{
	/// <summary>
	/// Applies renaming logic to the Target document (renaming existing schemas).
	/// </summary>
	/// <param name="targetDocument">The target OpenAPI document to modify.</param>
	/// <param name="targetRenames">Dictionary mapping old schema names to new names.</param>
	/// <param name="schemaOrigins">Dictionary tracking the origin of each schema.</param>
	void ApplyRenamesToTarget(
		OpenApiDocument targetDocument,
		Dictionary<string, string> targetRenames,
		Dictionary<string, SchemaOrigin> schemaOrigins);

	/// <summary>
	/// Applies renaming logic to the Source document (renaming incoming schemas before merge).
	/// </summary>
	/// <param name="sourceDocument">The source OpenAPI document to modify.</param>
	/// <param name="sourceRenames">Dictionary mapping old schema names to new names.</param>
	void ApplyRenamesToSource(
		OpenApiDocument sourceDocument,
		Dictionary<string, string> sourceRenames);
}
