using Koalesce.Services.Report;

namespace Koalesce.Services.ConflictResolution;

/// <summary>
/// Service responsible for coordinating security scheme conflict resolution between source and target OpenAPI documents.
/// Identical schemes are deduplicated; conflicting schemes are renamed using the same VirtualPrefix-based rules
/// applied to schema conflicts.
/// </summary>
internal class SecuritySchemeConflictCoordinator
{
	private readonly ILogger<SecuritySchemeConflictCoordinator> _logger;
	private readonly IConflictResolutionStrategy _strategy;

	public SecuritySchemeConflictCoordinator(
		ILogger<SecuritySchemeConflictCoordinator> logger,
		IConflictResolutionStrategy strategy)
	{
		_logger = logger;
		_strategy = strategy;
	}

	/// <summary>
	/// Orchestrates the security scheme conflict resolution process between source and target documents.
	/// Must be called before path merging so security references in operations are correct.
	/// </summary>
	public void ResolveConflicts(
		OpenApiDocument sourceDocument,
		OpenApiDocument targetDocument,
		string apiName,
		string? virtualPrefix,
		string? schemaConflictPattern,
		Dictionary<string, SchemaOrigin> securitySchemeOrigins,
		MergeReportBuilder reportBuilder)
	{
		if (sourceDocument.Components?.SecuritySchemes is null || targetDocument.Components?.SecuritySchemes is null)
			return;

		schemaConflictPattern ??= CoreConstants.DefaultSchemaConflictPattern;

		var sourceRenames = new Dictionary<string, string>();
		var targetRenames = new Dictionary<string, string>();
		var schemesToKeepOriginalName = new HashSet<string>();

		// Detection and Decision Phase
		foreach (var (key, sourceScheme) in sourceDocument.Components.SecuritySchemes)
		{
			if (!targetDocument.Components.SecuritySchemes.TryGetValue(key, out var targetScheme))
				continue;

			// Identical schemes are deduplicated (common in microservices sharing the same auth provider)
			if (AreSemanticallyEqual(sourceScheme, targetScheme))
			{
				reportBuilder.AddDeduplicatedSecurityScheme(key, apiName);
				_logger.LogDebug(
					"SecurityScheme '{Key}' from '{ApiName}' is identical to existing — deduplicated.",
					key, apiName);
				continue;
			}

			securitySchemeOrigins.TryGetValue(key, out var existingOrigin);

			var context = new SchemaConflictContext(
				key, apiName, virtualPrefix, existingOrigin, schemaConflictPattern);

			var decision = _strategy.DetermineResolution(context);

			ApplyResolutionDecision(
				decision, key, apiName, existingOrigin,
				sourceRenames, targetRenames, schemesToKeepOriginalName, reportBuilder);
		}

		// Apply renames to target
		ApplyRenames(targetDocument.Components.SecuritySchemes, targetRenames);
		UpdateOrigins(securitySchemeOrigins, targetRenames);
		RewriteSecurityReferences(targetDocument, targetRenames);

		// Restore "Keep Original" schemes (source takes over the original name)
		foreach (var key in schemesToKeepOriginalName)
		{
			if (sourceDocument.Components.SecuritySchemes.TryGetValue(key, out var scheme))
			{
				targetDocument.Components.SecuritySchemes[key] = scheme;
				securitySchemeOrigins[key] = new SchemaOrigin(apiName, virtualPrefix);
			}
		}

		// Apply renames to source
		ApplyRenames(sourceDocument.Components.SecuritySchemes, sourceRenames);
		RewriteSecurityReferences(sourceDocument, sourceRenames);

		// Track origins for renamed source keys
		foreach (var (_, newKey) in sourceRenames)
			securitySchemeOrigins[newKey] = new SchemaOrigin(apiName, virtualPrefix);
	}

	/// <summary>
	/// Compares two security schemes by their functional properties.
	/// Two schemes pointing to the same auth provider should be deduplicated, not renamed.
	/// </summary>
	private static bool AreSemanticallyEqual(IOpenApiSecurityScheme source, IOpenApiSecurityScheme target)
	{
		if (source is not OpenApiSecurityScheme a || target is not OpenApiSecurityScheme b)
			return false;

		return a.Type == b.Type
			&& string.Equals(a.Scheme, b.Scheme, StringComparison.OrdinalIgnoreCase)
			&& string.Equals(a.Name, b.Name, StringComparison.OrdinalIgnoreCase)
			&& a.In == b.In
			&& string.Equals(a.BearerFormat, b.BearerFormat, StringComparison.OrdinalIgnoreCase)
			&& a.OpenIdConnectUrl == b.OpenIdConnectUrl;
	}

	private void ApplyResolutionDecision(
		ResolutionDecision decision,
		string originalKey,
		string apiName,
		SchemaOrigin? existingOrigin,
		Dictionary<string, string> sourceRenames,
		Dictionary<string, string> targetRenames,
		HashSet<string> schemesToKeep,
		MergeReportBuilder reportBuilder)
	{
		switch (decision.Type)
		{
			case ConflictResolutionType.RenameBoth:
				if (decision.NewExistingKey != null)
					targetRenames[originalKey] = decision.NewExistingKey;

				sourceRenames[originalKey] = decision.NewCurrentKey;

				reportBuilder.AddSecuritySchemeConflict(
					originalKey, decision.NewCurrentKey,
					ConflictResolutionType.RenameBoth, apiName);

				_logger.LogInformation(
					"SecurityScheme collision '{Key}': Renaming existing to '{Existing}' and new to '{New}'",
					originalKey, decision.NewExistingKey, decision.NewCurrentKey);
				break;

			case ConflictResolutionType.RenameExisting:
				if (decision.NewExistingKey != null)
					targetRenames[originalKey] = decision.NewExistingKey;

				schemesToKeep.Add(originalKey);

				reportBuilder.AddSecuritySchemeConflict(
					originalKey, originalKey,
					ConflictResolutionType.RenameExisting, apiName);

				_logger.LogInformation(
					"SecurityScheme collision '{Key}': Renaming existing to '{Existing}' (new keeps original)",
					originalKey, decision.NewExistingKey);
				break;

			case ConflictResolutionType.RenameCurrent:
				sourceRenames[originalKey] = decision.NewCurrentKey;

				reportBuilder.AddSecuritySchemeConflict(
					originalKey, decision.NewCurrentKey,
					ConflictResolutionType.RenameCurrent, apiName);

				_logger.LogInformation(
					"SecurityScheme collision '{Key}': Renaming new to '{New}'",
					originalKey, decision.NewCurrentKey);
				break;
		}
	}

	private static void ApplyRenames(
		IDictionary<string, IOpenApiSecurityScheme> schemes,
		Dictionary<string, string> renames)
	{
		foreach (var (oldKey, newKey) in renames)
		{
			if (schemes.TryGetValue(oldKey, out var scheme))
			{
				schemes.Remove(oldKey);
				schemes[newKey] = scheme;
			}
		}
	}

	private static void UpdateOrigins(
		Dictionary<string, SchemaOrigin> origins,
		Dictionary<string, string> renames)
	{
		foreach (var (oldKey, newKey) in renames)
		{
			if (origins.TryGetValue(oldKey, out var origin))
			{
				origins.Remove(oldKey);
				origins[newKey] = origin;
			}
		}
	}

	private static void RewriteSecurityReferences(
		OpenApiDocument document,
		Dictionary<string, string> renames)
	{
		if (renames.Count == 0)
			return;

		RewriteSecurityRequirements(document, document.Security, renames);

		if (document.Paths is null)
			return;

		foreach (var pathItem in document.Paths.Values)
		{
			if (pathItem.Operations is null)
				continue;

			foreach (var operation in pathItem.Operations.Values)
				RewriteSecurityRequirements(document, operation.Security, renames);
		}
	}

	private static void RewriteSecurityRequirements(
		OpenApiDocument document,
		IList<OpenApiSecurityRequirement>? requirements,
		Dictionary<string, string> renames)
	{
		if (requirements is null)
			return;

		foreach (var requirement in requirements)
		{
			var keysToRename = requirement.Keys
				.OfType<OpenApiSecuritySchemeReference>()
				.Where(r => r.Reference?.Id is not null && renames.ContainsKey(r.Reference.Id))
				.ToList();

			foreach (var oldKey in keysToRename)
			{
				var scopes = requirement[oldKey];
				requirement.Remove(oldKey);
				requirement[new OpenApiSecuritySchemeReference(renames[oldKey.Reference!.Id!], document, null)] = scopes;
			}
		}
	}
}
