namespace Koalesce.OpenAPI.Services.ConflictResolution;

/// <summary>
/// Service responsible for coordinating schema conflict resolution between source and target OpenAPI documents.
/// </summary>
internal class SchemaConflictCoordinator
{
	private readonly ILogger<SchemaConflictCoordinator> _logger;
	private readonly IConflictResolutionStrategy _strategy;
	private readonly SchemaRenamer _renamer;

	public SchemaConflictCoordinator(
		ILogger<SchemaConflictCoordinator> logger,
		IConflictResolutionStrategy strategy,
		SchemaRenamer renamer)
	{
		_logger = logger;
		_strategy = strategy;
		_renamer = renamer;
	}

	/// <summary>
	/// Orchestrates the conflict resolution process between source and target documents.
	/// </summary>
	public void ResolveConflicts(
		OpenApiDocument sourceDocument,
		OpenApiDocument targetDocument,
		string apiName,
		string? virtualPrefix,
		string? schemaConflictPattern,
		Dictionary<string, SchemaOrigin> schemaOrigins)
	{
		if (sourceDocument.Components?.Schemas is null || targetDocument.Components?.Schemas is null)
			return;

		schemaConflictPattern ??= CoreConstants.DefaultSchemaConflictPattern;

		var sourceRenames = new Dictionary<string, string>();
		var targetRenames = new Dictionary<string, string>();
		var schemasToKeepOriginalName = new HashSet<string>();

		// Detection and Decision Phase
		foreach (var (key, _) in sourceDocument.Components.Schemas)
		{
			// If no collision, skip
			if (!targetDocument.Components.Schemas.ContainsKey(key))
				continue;

			schemaOrigins.TryGetValue(key, out var existingOrigin);

			var context = new SchemaConflictContext(
				key,
				apiName,
				virtualPrefix,
				existingOrigin,
				schemaConflictPattern
			);

			ResolutionDecision decision = _strategy.DetermineResolution(context);

			// Applying resolution decision
			ApplyResolutionDecision(
				decision, 
				key, 
				apiName, 
				schemaConflictPattern, 
				targetDocument, 
				sourceRenames, 
				targetRenames, 
				schemasToKeepOriginalName);
		}

		// Applying renames to Target Document
		_renamer.ApplyRenamesToTarget(targetDocument, targetRenames, schemaOrigins);

		// Restoring "Keep Original" Schemas
		// These are source schemas that replaced existing target schemas
		foreach (var key in schemasToKeepOriginalName)
		{
			if (sourceDocument.Components.Schemas.TryGetValue(key, out var schema))
			{
				targetDocument.Components.Schemas[key] = schema;
				schemaOrigins[key] = new SchemaOrigin(apiName, virtualPrefix);
				_logger.LogInformation("Schema '{Key}' taken over by '{ApiName}' (original name kept).", key, apiName);
			}
		}

		// Applying renames to Source Document
		_renamer.ApplyRenamesToSource(sourceDocument, sourceRenames);

		// Update Origins for new Source Keys
		foreach (var (oldKey, newKey) in sourceRenames)
		{
			schemaOrigins[newKey] = new SchemaOrigin(apiName, virtualPrefix);
		}
	}

	/// <summary>
	/// Applies the resolution decision to the renaming dictionaries
	/// </summary>
	private void ApplyResolutionDecision(
		ResolutionDecision decision,
		string originalKey,
		string apiName,
		string conflictPattern,
		OpenApiDocument targetDocument,
		Dictionary<string, string> sourceRenames,
		Dictionary<string, string> targetRenames,
		HashSet<string> schemasToKeep)
	{
		// Helper method to ensure uniqueness logic is applied to the decision result
		string UniqueCheck(string newKey) =>
			SchemaNameUtils.EnsureUniqueKey(newKey, originalKey, apiName, conflictPattern, targetDocument, targetRenames);

		switch (decision.Type)
		{
			case ConflictResolutionType.RenameBoth:
				if (decision.NewExistingKey != null)
				{
					// For target renames, check if the new name exists in document
					targetRenames[originalKey] = decision.NewExistingKey;
				}

				string uniqueBoth = UniqueCheck(decision.NewCurrentKey);
				sourceRenames[originalKey] = uniqueBoth;

				_logger.LogInformation("Schema collision '{Key}': Renaming existing to '{Existing}' and new to '{New}'",
					originalKey, decision.NewExistingKey, uniqueBoth);
				break;

			case ConflictResolutionType.RenameExisting:
				if (decision.NewExistingKey != null)
					targetRenames[originalKey] = decision.NewExistingKey;

				schemasToKeep.Add(originalKey);
				_logger.LogInformation("Schema collision '{Key}': Renaming existing to '{Existing}' (New keeps original)",
					originalKey, decision.NewExistingKey);
				break;

			case ConflictResolutionType.RenameCurrent:
				string uniqueCurrent = UniqueCheck(decision.NewCurrentKey);
				sourceRenames[originalKey] = uniqueCurrent;

				_logger.LogInformation("Schema collision '{Key}': Renaming new to '{New}'", originalKey, uniqueCurrent);
				break;
		}
	}
}