namespace Koalesce.Services;

/// <summary>
/// Handles post-merge finalization of an OpenAPI document and removes orphaned elements and consolidates server specs.
/// </summary>
internal class OpenApiDocumentFinalizer
{
	private readonly KoalesceOptions _options;
	private readonly SchemaReferenceWalker _schemaReferenceWalker;
	private readonly ILogger<OpenApiDocumentFinalizer> _logger;

	public OpenApiDocumentFinalizer(
		IOptions<KoalesceOptions> options,
		SchemaReferenceWalker schemaReferenceWalker,
		ILogger<OpenApiDocumentFinalizer> logger)
	{
		_options = options.Value;
		_schemaReferenceWalker = schemaReferenceWalker;
		_logger = logger;
	}

	/// <summary>
	/// Runs all post-merge cleanup steps on the merged document.
	/// </summary>
	public void Finalize(OpenApiDocument document)
	{
		RemoveOrphanedTags(document);
		RemoveOrphanedSchemas(document);
		RemoveOrphanedSecuritySchemes(document);
		ConsolidateServerSpecifications(document);
	}

	private void RemoveOrphanedTags(OpenApiDocument document)
	{
		if (document.Tags is null || document.Tags.Count == 0)
			return;

		var referenced = document.Paths?.Values
			.Where(p => p.Operations is not null)
			.SelectMany(p => p.Operations!.Values)
			.Where(o => o.Tags is not null)
			.SelectMany(o => o.Tags!)
			.Select(t => t.Name)
			.OfType<string>()
			.ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];

		var orphanedTags = document.Tags
			.Where(t => t.Name != null && !referenced.Contains(t.Name))
			.ToList();

		foreach (var tag in orphanedTags)
		{
			document.Tags.Remove(tag);
			_logger.LogInformation("Removed orphaned tag '{TagName}' (not referenced by any operation)", tag.Name);
		}
	}

	private void RemoveOrphanedSchemas(OpenApiDocument document)
	{
		if (document.Components?.Schemas is null || document.Components.Schemas.Count == 0)
			return;

		var referenced = _schemaReferenceWalker.CollectReferencedSchemas(document);
		foreach (var key in document.Components.Schemas.Keys.Where(k => !referenced.Contains(k)).ToList())
		{
			document.Components.Schemas.Remove(key);
			_logger.LogInformation("Removed orphaned schema '{SchemaName}' (not referenced by any path)", key);
		}
	}

	private void RemoveOrphanedSecuritySchemes(OpenApiDocument document)
	{
		if (document.Components?.SecuritySchemes is null || document.Components.SecuritySchemes.Count == 0)
			return;

		static IEnumerable<string> GetSchemeIds(IList<OpenApiSecurityRequirement>? reqs)
			=> reqs?.SelectMany(r => r.Keys.OfType<OpenApiSecuritySchemeReference>())
				.Select(k => k.Reference?.Id).OfType<string>() ?? [];

		var referenced = GetSchemeIds(document.Security)
			.Concat(document.Paths?.Values
				.Where(p => p.Operations is not null)
				.SelectMany(p => p.Operations!.Values)
				.SelectMany(o => GetSchemeIds(o.Security)) ?? [])
			.ToHashSet();

		foreach (var key in document.Components.SecuritySchemes.Keys.Where(k => !referenced.Contains(k)).ToList())
		{
			document.Components.SecuritySchemes.Remove(key);
			_logger.LogInformation("Removed orphaned security scheme '{SchemeName}' (not referenced by any security requirement)", key);
		}
	}

	private void ConsolidateServerSpecifications(OpenApiDocument doc)
	{
		doc.Servers ??= [];
		if (!string.IsNullOrEmpty(_options.ApiGatewayBaseUrl))
		{
			doc.Servers.Clear();
			doc.Servers.Add(new OpenApiServer { Url = _options.ApiGatewayBaseUrl, Description = "API Gateway" });
		}
		else if (doc.Servers.Count == 0)
		{
			doc.Servers.Add(new OpenApiServer { Url = "/" });
		}
	}
}
