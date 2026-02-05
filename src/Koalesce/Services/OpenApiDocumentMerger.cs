namespace Koalesce.Services;

/// <summary>
/// Service for merging multiple OpenAPI documents into a single consolidated document
/// </summary>
internal class OpenApiDocumentMerger
{
	private readonly ILogger<OpenApiDocumentMerger> _logger;
	private readonly KoalesceOptions _options;
	private readonly OpenApiDefinitionLoader _loader;
	private readonly OpenApiPathMerger _pathMerger;
	private readonly SchemaConflictCoordinator _schemaConflictCoordinator;

	public OpenApiDocumentMerger(
		IOptions<KoalesceOptions> options,
		ILogger<OpenApiDocumentMerger> logger,
		OpenApiDefinitionLoader loader,
		OpenApiPathMerger pathMerger,
		SchemaConflictCoordinator schemaConflictCoordinator)
	{
		ArgumentNullException.ThrowIfNull(options);
		ArgumentNullException.ThrowIfNull(logger);

		_options = options.Value;
		_logger = logger;
		_loader = loader;
		_pathMerger = pathMerger;
		_schemaConflictCoordinator = schemaConflictCoordinator;
	}

	/// <summary>
	/// Builds a single API definition document from multiple API specifications.
	/// </summary>
	/// <returns>A tuple containing the merged document and the load results for each source.</returns>
	public async Task<(OpenApiDocument Document, IReadOnlyList<SourceLoadResult> SourceResults)> MergeIntoSingleDefinitionAsync()
	{
		if (_options.Sources?.Any() != true)
			throw new ArgumentException("API source list cannot be empty.");

		_logger.LogInformation("Starting API Koalescing process with {Count} APIs", _options.Sources.Count);

		// Schema origins tracked per merge operation (not shared across requests)
		var schemaOrigins = new Dictionary<string, SchemaOrigin>();
		var sourceResults = new List<SourceLoadResult>();

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
				sourceResults.Add(new SourceLoadResult(apiSource, isLoaded, errorMessage));

				if (isLoaded)
					MergeApiDefinition(downstreamDoc!, mergedDocument, apiSource, schemaOrigins);
			}

			// Finalize
			ConsolidateServerDefinitions(mergedDocument);

			_logger.LogInformation("API Koalescing completed.");
			return (mergedDocument, sourceResults);
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
		var version = _options.MergedEndpoint?.ExtractVersionFromPath();
		return new OpenApiDocument
		{
			Info = new OpenApiInfo
			{
				Title = _options.Title,
				Version = version ?? KoalesceConstants.V1
			},
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
	/// Merges a specific API definition into the target document, applying prefixes and isolation rules
	/// </summary>
	private void MergeApiDefinition(
		OpenApiDocument sourceDoc,
		OpenApiDocument targetDoc,
		ApiSource apiSource,
		Dictionary<string, SchemaOrigin> schemaOrigins)
	{
		string apiName = sourceDoc.Info?.Title ?? KoalesceConstants.UnknownApi;
		string apiVersion = sourceDoc.Info?.Version ?? KoalesceConstants.V1;

		// Resolve Conflicts
		_schemaConflictCoordinator.ResolveConflicts(
			sourceDoc, targetDoc, apiName, apiSource.VirtualPrefix,
			_options.SchemaConflictPattern, schemaOrigins);

		// Merge Servers (unless using API Gateway)
		var serverEntry = string.IsNullOrEmpty(_options.ApiGatewayBaseUrl)
			? MergeServers(sourceDoc, targetDoc, apiSource, apiName, apiVersion)
			: null;

		// Merge Paths
		_pathMerger.MergePaths(sourceDoc, targetDoc.Paths, apiName, apiSource, serverEntry);

		// Merge Components & Tags
		MergeComponents(sourceDoc.Components, targetDoc.Components, apiName, apiSource.VirtualPrefix, schemaOrigins);
		MergeTags(sourceDoc, targetDoc, apiSource);
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
		Dictionary<string, SchemaOrigin> schemaOrigins)
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
				targetComponents.SecuritySchemes.TryAdd(key, securityScheme);
			}
		}
	}

	/// <summary>
	/// Merges tags prioritizing: Operation Tags > Document Global Tags > Source Fallback
	/// </summary>
	private static void MergeTags(
		OpenApiDocument sourceDoc,
		OpenApiDocument targetDoc,
		ApiSource apiSource)
	{
		// Use HashSet for O(1) lookup
		var existingTagNames = new HashSet<string>(
			targetDoc.Tags?.Select(t => t.Name).Where(n => n != null).Cast<string>() ?? []);

		if (sourceDoc.Tags != null)
		{
			foreach (var tag in sourceDoc.Tags)
			{
				if (tag.Name != null && existingTagNames.Add(tag.Name))
					targetDoc.Tags?.Add(tag);
			}
		}

		if (sourceDoc.Paths is null)
			return;

		// Derive default tag name from URL host or file name
		var defaultTagName = GetDefaultTagName(apiSource);

		foreach (var path in sourceDoc.Paths.Values)
		{
			if (path.Operations is null)
				continue;

			foreach (var operation in path.Operations.Values)
			{
				operation.Tags ??= new HashSet<OpenApiTagReference>();
				if (operation.Tags.Count == 0)
				{
					if (sourceDoc.Tags?.Count > 0)
					{
						foreach (var t in sourceDoc.Tags)
						{
							if (t.Name != null)
								operation.Tags.Add(new OpenApiTagReference(t.Name));
						}
					}
					else
					{
						operation.Tags.Add(new OpenApiTagReference(defaultTagName));
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
	/// Consolidates the server definitions in the specified OpenAPI document to ensure a valid server entry is present.
	/// </summary>
	private void ConsolidateServerDefinitions(OpenApiDocument doc)
	{
		doc.Servers ??= [];
		if (!string.IsNullOrEmpty(_options.ApiGatewayBaseUrl))
		{
			doc.Servers.Clear();
			doc.Servers.Add(new OpenApiServer { Url = _options.ApiGatewayBaseUrl, Description = "API Gateway" });
		}
		else if (!doc.Servers.Any())
		{
			doc.Servers.Add(new OpenApiServer { Url = "/" });
		}
	}
}