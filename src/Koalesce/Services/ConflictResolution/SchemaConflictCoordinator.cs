namespace Koalesce.Services.ConflictResolution;

/// <summary>
/// Service responsible for coordinating schema conflict resolution between source and target OpenAPI documents.
/// </summary>
internal class SchemaConflictCoordinator
{
	private readonly ILogger<SchemaConflictCoordinator> _logger;
	private readonly IConflictResolutionStrategy _strategy;
	private readonly ISchemaRenamer _renamer;

	public SchemaConflictCoordinator(
		ILogger<SchemaConflictCoordinator> logger,
		IConflictResolutionStrategy strategy,
		ISchemaRenamer renamer)
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
		Dictionary<string, SchemaOrigin> schemaOrigins,
		MergeReportBuilder reportBuilder)
	{
		if (sourceDocument.Components?.Schemas is null || targetDocument.Components?.Schemas is null)
			return;

		schemaConflictPattern ??= CoreConstants.DefaultSchemaConflictPattern;

		var sourceRenames = new Dictionary<string, string>();
		var targetRenames = new Dictionary<string, string>();
		var schemasToKeepOriginalName = new HashSet<string>();

		// Detection and Decision Phase
		foreach (var (key, sourceSchema) in sourceDocument.Components.Schemas)
		{
			// If no collision, skip
			if (!targetDocument.Components.Schemas.TryGetValue(key, out var targetSchema))
				continue;
			
			if (AreStructurallyIdentical(sourceSchema, targetSchema))
			{
				reportBuilder.AddDeduplicatedSchema(key, apiName);
				_logger.LogDebug(
					"Schema '{Key}' from '{ApiName}' is structurally identical to existing — deduplicated.",
					key, apiName);
				continue;
			}

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
				existingOrigin,
				schemaConflictPattern,
				targetDocument,
				sourceRenames,
				targetRenames,
				schemasToKeepOriginalName,
				reportBuilder);
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
	/// Compares two schemas by serializing them to JSON and comparing the result.
	/// Handles all nesting, composition keywords, and additionalProperties.
	/// </summary>
	private static bool AreStructurallyIdentical(IOpenApiSchema source, IOpenApiSchema target)
	{
		using var sw1 = new StringWriter();
		using var sw2 = new StringWriter();
		source.SerializeAsV3(new OpenApiJsonWriter(sw1));
		target.SerializeAsV3(new OpenApiJsonWriter(sw2));
		return sw1.ToString() == sw2.ToString();
	}

	/// <summary>
	/// Applies the resolution decision to the renaming dictionaries
	/// </summary>
	private void ApplyResolutionDecision(
		ResolutionDecision decision,
		string originalKey,
		string apiName,
		SchemaOrigin? existingOrigin,
		string conflictPattern,
		OpenApiDocument targetDocument,
		Dictionary<string, string> sourceRenames,
		Dictionary<string, string> targetRenames,
		HashSet<string> schemasToKeep,
		MergeReportBuilder reportBuilder)
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

				reportBuilder.AddSchemaConflict(
					originalKey, uniqueBoth,
					ConflictResolutionType.RenameBoth, apiName);

				_logger.LogInformation("Schema collision '{Key}': Renaming existing to '{Existing}' and new to '{New}'",
					originalKey, decision.NewExistingKey, uniqueBoth);
				break;

			case ConflictResolutionType.RenameExisting:
				if (decision.NewExistingKey != null)
					targetRenames[originalKey] = decision.NewExistingKey;

				schemasToKeep.Add(originalKey);

				reportBuilder.AddSchemaConflict(
					originalKey, originalKey,
					ConflictResolutionType.RenameExisting, apiName);

				_logger.LogInformation("Schema collision '{Key}': Renaming existing to '{Existing}' (New keeps original)",
					originalKey, decision.NewExistingKey);
				break;

			case ConflictResolutionType.RenameCurrent:
				string uniqueCurrent = UniqueCheck(decision.NewCurrentKey);
				sourceRenames[originalKey] = uniqueCurrent;

				reportBuilder.AddSchemaConflict(
					originalKey, uniqueCurrent,
					ConflictResolutionType.RenameCurrent, apiName);

				_logger.LogInformation("Schema collision '{Key}': Renaming new to '{New}'", originalKey, uniqueCurrent);
				break;
		}
	}
}