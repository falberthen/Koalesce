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
	private readonly OpenApiDocumentFinalizer _finalizer;

	public OpenApiDocumentMerger(
		IOptions<KoalesceOptions> options,
		ILogger<OpenApiDocumentMerger> logger,
		OpenApiDefinitionLoader loader,
		OpenApiPathMerger pathMerger,
		SchemaConflictCoordinator schemaConflictCoordinator,
		SecuritySchemeConflictCoordinator securitySchemeConflictCoordinator,
		OpenApiDocumentFinalizer finalizer)
	{
		ArgumentNullException.ThrowIfNull(options);
		ArgumentNullException.ThrowIfNull(logger);

		_options = options.Value;
		_logger = logger;
		_loader = loader;
		_pathMerger = pathMerger;
		_schemaConflictCoordinator = schemaConflictCoordinator;
		_securitySchemeConflictCoordinator = securitySchemeConflictCoordinator;
		_finalizer = finalizer;
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
			_finalizer.Finalize(mergedDocument);

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
	private static void MergeComponents(
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
					schemaOrigins.TryAdd(key, new SchemaOrigin(apiName, virtualPrefix));
			}
		}

		// Merge Security Schemes
		if (sourceComponents.SecuritySchemes is not null)
		{
			targetComponents.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
			foreach (var (key, securityScheme) in sourceComponents.SecuritySchemes)
			{
				if (targetComponents.SecuritySchemes.TryAdd(key, securityScheme))
					securitySchemeOrigins.TryAdd(key, new SchemaOrigin(apiName, virtualPrefix));
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
		var existingTagNames = targetDoc.Tags?.Select(t => t.Name).OfType<string>().ToHashSet() ?? [];

		// Prefix and register document-level tags from source
		if (sourceDoc.Tags != null)
		{
			foreach (var tag in sourceDoc.Tags.Where(t => t.Name != null))
			{
				tag.Name = ApplyTagPrefix(tag.Name!, prefix);
				if (existingTagNames.Add(tag.Name))
					targetDoc.Tags?.Add(tag);
			}
		}

		if (sourceDoc.Paths is null)
			return;

		foreach (var (pathKey, path) in sourceDoc.Paths)
		{
			if (path.Operations is null) 
				continue;

			// Skip paths that were excluded during path merging
			if (OpenApiPathMerger.GetMatchedExclusionPattern(pathKey, apiSource.ExcludePaths) is not null) 
				continue;

			foreach (var operation in path.Operations.Values)
			{
				operation.Tags ??= new HashSet<OpenApiTagReference>();

				// Apply prefix to existing operation tags
				if (prefix != null && operation.Tags.Count > 0) 
				{ 
					operation.Tags = operation.Tags.Select(t => 
						new OpenApiTagReference(ApplyTagPrefix(t.Name!, prefix)))
						.ToHashSet();
				}

				if (operation.Tags.Count == 0)
				{
					// Priority 1: Document Tags (previously prefixed)
					if (sourceDoc.Tags?.Count > 0) { 
						foreach (var t in sourceDoc.Tags.Where(t => t.Name != null))
							operation.Tags.Add(new OpenApiTagReference(t.Name!));
					}
					// Priority 2: Only generate tag in a merged context to avoid unnecessary tags for single-source scenarios
					else if (isMergeContext)
					{
						operation.Tags.Add
							(new OpenApiTagReference(!string.IsNullOrEmpty(prefix)
							? prefix
							: GetDefaultTagName(apiSource))
						);
					}
				}

				// Add referenced tags to document-level tags
				foreach (var tagRef in operation.Tags.Where(t => t.Name != null && existingTagNames.Add(t.Name!)))
					targetDoc.Tags?.Add(new OpenApiTag { Name = tagRef.Name });
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
			return Path.GetFileNameWithoutExtension(apiSource.FilePath).CleanName();

		return KoalesceConstants.UnknownTagName;
	}
}
