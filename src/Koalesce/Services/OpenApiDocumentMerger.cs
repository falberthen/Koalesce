using Koalesce.Services.Report;

namespace Koalesce.Services;

/// <summary>
/// Service for merging multiple OpenAPI documents into a single consolidated specification
/// </summary>
internal class OpenApiDocumentMerger
{
	private readonly ILogger<OpenApiDocumentMerger> _logger;
	private readonly KoalesceOptions _options;
	private readonly OpenApiDefinitionLoader _loader;
	private readonly OpenApiPathMerger _pathMerger;
	private readonly SchemaConflictCoordinator _schemaConflictCoordinator;
	private readonly SecuritySchemeConflictCoordinator _securitySchemeConflictCoordinator;
	private readonly ISchemaReferenceWalker _schemaReferenceWalker;

	public OpenApiDocumentMerger(
		IOptions<KoalesceOptions> options,
		ILogger<OpenApiDocumentMerger> logger,
		OpenApiDefinitionLoader loader,
		OpenApiPathMerger pathMerger,
		SchemaConflictCoordinator schemaConflictCoordinator,
		SecuritySchemeConflictCoordinator securitySchemeConflictCoordinator,
		ISchemaReferenceWalker schemaReferenceWalker)
	{
		ArgumentNullException.ThrowIfNull(options);
		ArgumentNullException.ThrowIfNull(logger);

		_options = options.Value;
		_logger = logger;
		_loader = loader;
		_pathMerger = pathMerger;
		_schemaConflictCoordinator = schemaConflictCoordinator;
		_securitySchemeConflictCoordinator = securitySchemeConflictCoordinator;
		_schemaReferenceWalker = schemaReferenceWalker;
	}

	/// <summary>
	/// Builds a single API definition document from multiple API specifications.
	/// </summary>
	/// <returns>A tuple containing the merged document, load results, and a structured merge report.</returns>
	public async Task<(OpenApiDocument Document, IReadOnlyList<SourceLoadResult> SourceResults, MergeReport Report)> MergeIntoSingleSpecificationAsync()
	{
		if (_options.Sources is null || _options.Sources.Count == 0)
			throw new ArgumentException("API source list cannot be empty.");

		_logger.LogInformation("Starting API Koalescing process with {Count} APIs", _options.Sources.Count);

		// Per-merge state (not shared across requests)
		var schemaOrigins = new Dictionary<string, SchemaOrigin>();
		var securitySchemeOrigins = new Dictionary<string, SchemaOrigin>();
		var sourceResults = new List<SourceLoadResult>();
		var reportBuilder = new MergeReportBuilder();

		try
		{
			// Initialize merged document
			var mergedDocument = InitializeMergedDocument();

			// Fetch concurrently using the Loader
			var fetchDocumentTasks = _options.Sources.Select(async source =>
			{
				var (doc, errorMessage) = await _loader.LoadAsync(source);
				return (ApiSource: source, Document: doc, ErrorMessage: errorMessage);
			});

			var loadResults = await Task.WhenAll(fetchDocumentTasks);

			// Track load results and merge successfully loaded documents
			foreach (var (apiSource, downstreamDoc, errorMessage) in loadResults)
			{
				bool isLoaded = downstreamDoc is not null;
				var sourceResult = new SourceLoadResult(apiSource, isLoaded, errorMessage);
				sourceResults.Add(sourceResult);
				reportBuilder.AddSource(sourceResult);

				if (isLoaded)
					MergeApiSpecification(downstreamDoc!, mergedDocument, apiSource, schemaOrigins, securitySchemeOrigins, reportBuilder);
			}

			// Finalize
			RemoveOrphanedSchemas(mergedDocument);
			RemoveOrphanedSecuritySchemes(mergedDocument);
			ConsolidateServerSpecifications(mergedDocument);

			_logger.LogInformation("API Koalescing completed.");
			return (mergedDocument, sourceResults, reportBuilder.Build());
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error during API Koalescing.");
			throw;
		}
	}

	/// <summary>
	/// Initializes a new OpenAPI document with default metadata, empty paths, components, servers, security requirements, and tags.
	/// </summary>
	private OpenApiDocument InitializeMergedDocument()
	{
		var info = _options.Info;

		// Only override version if user didn't explicitly set one (still has default value)
		if (info.Version is null)
			info.Version = _options.MergedEndpoint?.ExtractVersionFromPath();

		return new OpenApiDocument
		{
			Info = info,
			Paths = new OpenApiPaths(),
			Components = new OpenApiComponents
			{
				Schemas = new Dictionary<string, IOpenApiSchema>(),
				SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>()
			},
			Servers = [],
			Tags = new HashSet<OpenApiTag>()
		};
	}

	/// <summary>
	/// Merges a given API specification into the target document, applying prefixes and isolation rules
	/// </summary>
	private void MergeApiSpecification(
		OpenApiDocument sourceDoc,
		OpenApiDocument targetDoc,
		ApiSource apiSource,
		Dictionary<string, SchemaOrigin> schemaOrigins,
		Dictionary<string, SchemaOrigin> securitySchemeOrigins,
		MergeReportBuilder reportBuilder)
	{
		string apiName = sourceDoc.Info?.Title ?? KoalesceConstants.UnknownApi;
		string apiVersion = sourceDoc.Info?.Version ?? KoalesceConstants.DefaultVersion;

		// Resolve Conflicts (same VirtualPrefix-based rules for both)
		_schemaConflictCoordinator.ResolveConflicts(
			sourceDoc, targetDoc, apiName, apiSource.VirtualPrefix,
			_options.SchemaConflictPattern, schemaOrigins, reportBuilder);

		_securitySchemeConflictCoordinator.ResolveConflicts(
			sourceDoc, targetDoc, apiName, apiSource.VirtualPrefix,
			_options.SchemaConflictPattern, securitySchemeOrigins, reportBuilder);

		// Merge Servers (unless using API Gateway)
		var serverEntry = string.IsNullOrEmpty(_options.ApiGatewayBaseUrl)
			? MergeServers(sourceDoc, targetDoc, apiSource, apiName, apiVersion)
			: null;

		// Merge Paths
		_pathMerger.MergePaths(sourceDoc, targetDoc.Paths, apiName, apiSource, serverEntry, reportBuilder);

		// Merge Components
		MergeComponents(sourceDoc.Components, targetDoc.Components, apiName, apiSource.VirtualPrefix, schemaOrigins, securitySchemeOrigins);

		// Merge Tags
		bool isMergedContext = _options.Sources.Count > 1;
		MergeTags(sourceDoc, targetDoc, apiSource, isMergedContext);
	}

	/// <summary>
	/// Merges server definitions from source to target document.
	/// Prefers servers declared in the source document; falls back to fetch URL for URL-based sources.
	/// </summary>
	private static OpenApiServer? MergeServers(
		OpenApiDocument sourceDoc,
		OpenApiDocument targetDoc,
		ApiSource apiSource,
		string apiName,
		string apiVersion)
	{
		targetDoc.Servers ??= [];
		OpenApiServer? serverEntry = null;

		// Prefer servers from source document
		if (sourceDoc.Servers?.Count > 0)
		{
			foreach (var server in sourceDoc.Servers)
			{
				if (targetDoc.Servers.Any(s => s.Url == server.Url))
					continue;

				var serverCopy = new OpenApiServer
				{
					Url = server.Url,
					Description = server.Description ?? $"{apiName} ({apiVersion})"
				};
				targetDoc.Servers.Add(serverCopy);
				serverEntry ??= serverCopy;
			}

			return serverEntry;
		}

		// Fallback for URL-based sources without declared servers
		if (string.IsNullOrWhiteSpace(apiSource.Url))
			return null;

		string baseUrl = new Uri(apiSource.Url).GetLeftPart(UriPartial.Authority);
		serverEntry = new OpenApiServer { Url = baseUrl, Description = $"{apiName} ({apiVersion})" };

		if (!targetDoc.Servers.Any(s => s.Url == baseUrl))
			targetDoc.Servers.Add(serverEntry);

		return serverEntry;
	}

	/// <summary>
	/// Merges schemas and security scheme definitions
	/// </summary>
	private void MergeComponents(
		OpenApiComponents? sourceComponents,
		OpenApiComponents? targetComponents,
		string apiName,
		string? virtualPrefix,
		Dictionary<string, SchemaOrigin> schemaOrigins,
		Dictionary<string, SchemaOrigin> securitySchemeOrigins)
	{
		if (sourceComponents is null || targetComponents is null)
			return;

		// Merge Schemas
		if (sourceComponents.Schemas is not null)
		{
			targetComponents.Schemas ??= new Dictionary<string, IOpenApiSchema>();
			foreach (var (key, schema) in sourceComponents.Schemas)
			{
				if (targetComponents.Schemas.TryAdd(key, schema))
				{
					schemaOrigins.TryAdd(key, new SchemaOrigin(apiName, virtualPrefix));
				}
			}
		}

		// Merge Security Schemes
		if (sourceComponents.SecuritySchemes is not null)
		{
			targetComponents.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
			foreach (var (key, securityScheme) in sourceComponents.SecuritySchemes)
			{
				if (targetComponents.SecuritySchemes.TryAdd(key, securityScheme))
				{
					securitySchemeOrigins.TryAdd(key, new SchemaOrigin(apiName, virtualPrefix));
				}
			}
		}
	}

	/// <summary>
	/// Merges tags prioritizing: Operation Tags > Document Global Tags > Source Fallback.
	/// When PrefixTagsWith is configured, all tags from the source are prefixed before merging.
	/// </summary>
	private static void MergeTags(
		OpenApiDocument sourceDoc,
		OpenApiDocument targetDoc,
		ApiSource apiSource,
		bool isMergeContext)
	{
		var prefix = apiSource.PrefixTagsWith;

		// Use HashSet for O(1) lookup
		var existingTagNames = new HashSet<string>(
			targetDoc.Tags?.Select(t => t.Name).Where(n => n != null).Cast<string>() ?? []);

		// Apply prefix to source document-level tags
		if (sourceDoc.Tags != null)
		{
			foreach (var tag in sourceDoc.Tags)
			{
				if (tag.Name != null)
					tag.Name = ApplyTagPrefix(tag.Name, prefix);

				if (tag.Name != null && existingTagNames.Add(tag.Name))
					targetDoc.Tags?.Add(tag);
			}
		}

		if (sourceDoc.Paths is null)
			return;

		foreach (var path in sourceDoc.Paths.Values)
		{
			if (path.Operations is null)
				continue;

			foreach (var operation in path.Operations.Values)
			{
				operation.Tags ??= new HashSet<OpenApiTagReference>();

				// Apply prefix to existing operation tags
				if (prefix != null && operation.Tags.Count > 0)
				{
					var prefixedTags = operation.Tags
						.Select(t => new OpenApiTagReference(ApplyTagPrefix(t.Name!, prefix)))
						.ToList();
					operation.Tags.Clear();
					foreach (var tag in prefixedTags)
						operation.Tags.Add(tag);
				}

				if (operation.Tags.Count == 0)
				{
					// Priority 1: Document Tags (previously prefixed)
					if (sourceDoc.Tags?.Count > 0)
					{
						foreach (var t in sourceDoc.Tags)
						{
							if (t.Name != null)
								operation.Tags.Add(new OpenApiTagReference(t.Name));
						}
					}
					// Priority 2: Only generate tag if in a merged context (multiple sources) to avoid unnecessary tags for single-source scenarios
					else if (isMergeContext)
					{
						// If prefix is configured, use it as the tag name; otherwise, derive a default tag name from the API source
						string tagName = !string.IsNullOrEmpty(prefix)
							? prefix
							: GetDefaultTagName(apiSource);

						operation.Tags.Add(new OpenApiTagReference(tagName));
					}					
				}

				// Add referenced tags to document-level tags
				foreach (var tagRef in operation.Tags)
				{
					if (tagRef.Name != null && existingTagNames.Add(tagRef.Name))
						targetDoc.Tags?.Add(new OpenApiTag { Name = tagRef.Name });
				}
			}
		}
	}

	/// <summary>
	/// Applies a prefix to a tag name if the prefix is configured.
	/// </summary>
	private static string ApplyTagPrefix(string tagName, string? prefix)
		=> prefix is null ? tagName : $"{prefix} - {tagName}";

	/// <summary>
	/// Gets the default tag name based on the API source (URL host or file name).
	/// </summary>
	private static string GetDefaultTagName(ApiSource apiSource)
	{
		if (!string.IsNullOrWhiteSpace(apiSource.Url))
			return new Uri(apiSource.Url).Host.Replace(".", "-");

		if (!string.IsNullOrWhiteSpace(apiSource.FilePath))
			return Path.GetFileNameWithoutExtension(apiSource.FilePath).CleanName(); // Use file name without extension as default tag

		return KoalesceConstants.UnknownTagName;
	}

	/// <summary>
	/// Removes schemas from the merged document that are not referenced by any path/operation.
	/// </summary>
	private void RemoveOrphanedSchemas(OpenApiDocument document)
	{
		if (document.Components?.Schemas is null || document.Components.Schemas.Count == 0)
			return;

		var referencedSchemas = _schemaReferenceWalker.CollectReferencedSchemas(document);
		var orphanedKeys = document.Components.Schemas.Keys
			.Where(key => !referencedSchemas.Contains(key))
			.ToList();

		foreach (var key in orphanedKeys)
		{
			document.Components.Schemas.Remove(key);
			_logger.LogInformation("Removed orphaned schema '{SchemaName}' (not referenced by any path)", key);
		}
	}

	/// <summary>
	/// Removes security schemes from the merged document that are not referenced by any security requirement.
	/// </summary>
	private void RemoveOrphanedSecuritySchemes(OpenApiDocument document)
	{
		if (document.Components?.SecuritySchemes is null || document.Components.SecuritySchemes.Count == 0)
			return;

		var referenced = new HashSet<string>();

		CollectSecuritySchemeRefs(document.Security, referenced);

		if (document.Paths is not null)
		{
			foreach (var pathItem in document.Paths.Values)
			{
				if (pathItem.Operations is null)
					continue;

				foreach (var operation in pathItem.Operations.Values)
					CollectSecuritySchemeRefs(operation.Security, referenced);
			}
		}

		var orphanedKeys = document.Components.SecuritySchemes.Keys
			.Where(key => !referenced.Contains(key))
			.ToList();

		foreach (var key in orphanedKeys)
		{
			document.Components.SecuritySchemes.Remove(key);
			_logger.LogInformation("Removed orphaned security scheme '{SchemeName}' (not referenced by any security requirement)", key);
		}
	}

	private static void CollectSecuritySchemeRefs(IList<OpenApiSecurityRequirement>? requirements, HashSet<string> referenced)
	{
		if (requirements is null)
			return;

		foreach (var requirement in requirements)
		{
			foreach (var key in requirement.Keys.OfType<OpenApiSecuritySchemeReference>())
			{
				if (key.Reference?.Id is not null)
					referenced.Add(key.Reference.Id);
			}
		}
	}

	/// <summary>
	/// Consolidates the server specifications in the specified OpenAPI document to ensure a valid server entry is present.
	/// </summary>
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