namespace Koalesce.OpenAPI.Services;

/// <summary>
/// Service for merging multiple OpenAPI documents into a single consolidated document
/// </summary>
internal class OpenApiDocumentMerger : IDocumentMerger<OpenApiDocument>
{
	private readonly ILogger<OpenApiDocumentMerger> _logger;
	private readonly KoalesceOpenApiOptions _options;
	private readonly OpenApiDefinitionLoader _loader;
	private readonly OpenApiPathMerger _pathMerger;
	private readonly SchemaConflictCoordinator _schemaConflictCoordinator;

	/// <summary>
	/// Tracks the origin of each schema for conflict resolution
	/// Key: schema name, Value: origin info (ApiName, VirtualPrefix)
	/// </summary>
	private readonly Dictionary<string, SchemaOrigin> _schemaOrigins = new();

	public OpenApiDocumentMerger(
		IOptions<KoalesceOpenApiOptions> options,
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
	public async Task<OpenApiDocument> MergeIntoSingleDefinitionAsync()
	{
		if (_options.Sources?.Any() != true)
			throw new ArgumentException("API source list cannot be empty.");

		_logger.LogInformation("Starting API Koalescing process with {Count} APIs", _options.Sources.Count);
		
		// Clear schema origins from previous runs
		_schemaOrigins.Clear();

		try
		{
			// Initialize merged document
			var mergedDocument = InitializeMergedDocument();

			// Fetch concurrently using the Loader
			var fetchDocumentTasks = _options.Sources.Select(async source =>
			{
				var doc = await _loader.LoadAsync(source.Url);
				return (ApiSource: source, Document: doc);
			});

			var loadResults = await Task.WhenAll(fetchDocumentTasks);

			// Merge results sequentially
			foreach (var (apiSource, downstreamDoc) in loadResults)
			{
				if (downstreamDoc is not null)
				{
					MergeApiDefinition(downstreamDoc, mergedDocument, apiSource);
				}
			}

			// Finalize
			ConsolidateServerDefinitions(mergedDocument);
			ConsolidateGlobalSecurity(mergedDocument);

			_logger.LogInformation("API Koalescing completed.");
			return mergedDocument;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error during API Koalescing.");
			throw;
		}
	}

	/// <summary>
	/// Initializes a new OpenAPI document with default metadata, empty paths, components, servers, security requirements,
	/// and tags.
	/// </summary>
	private OpenApiDocument InitializeMergedDocument()
	{
		var version = _options.MergedDocumentPath.ExtractVersionFromPath();
		return new OpenApiDocument
		{
			Info = new OpenApiInfo
			{
				Title = _options.Title,
				Version = version ?? OpenAPIConstants.V1
			},
			Paths = new OpenApiPaths(),
			Components = new OpenApiComponents
			{
				SecuritySchemes = new Dictionary<string, OpenApiSecurityScheme>(),
				Schemas = new Dictionary<string, OpenApiSchema>()
			},
			Servers = new List<OpenApiServer>(),
			SecurityRequirements = new List<OpenApiSecurityRequirement>(),
			Tags = new List<OpenApiTag>()
		};
	}

	/// <summary>
	/// Merges a specific API definition into the target document, applying prefixes and isolation rules
	/// </summary>
	private void MergeApiDefinition(
		OpenApiDocument sourceDoc,
		OpenApiDocument targetDoc,
		ApiSource apiSource)
	{
		string apiName = sourceDoc.Info?.Title ?? "Unknown API";
		string apiVersion = sourceDoc.Info?.Version ?? "v1";

		// Resolve Conflicts
		_schemaConflictCoordinator.ResolveConflicts(
			sourceDoc, targetDoc, apiName, apiSource.VirtualPrefix,
			_options.SchemaConflictPattern, _schemaOrigins);

		// Prepare Server Entry (for Aggregation Mode)
		OpenApiServer? serverEntry = null;
		if (string.IsNullOrEmpty(_options.ApiGatewayBaseUrl))
		{
			string baseUrl = new Uri(apiSource.Url).GetLeftPart(UriPartial.Authority);
			serverEntry = new OpenApiServer { Url = baseUrl, Description = $"{apiName} ({apiVersion})" };

			if (!targetDoc.Servers.Any(s => s.Url == baseUrl))
				targetDoc.Servers.Add(serverEntry);
		}

		// Merge Paths
		_pathMerger.MergePaths(sourceDoc, targetDoc.Paths, apiName, apiSource, serverEntry);

		// Merge Components & Tags
		MergeComponents(sourceDoc.Components, targetDoc.Components, apiName, apiSource.VirtualPrefix);
		MergeTags(sourceDoc, targetDoc, apiSource.Url);
	}
	
	/// <summary>
	/// Merges schemas and security scheme definitions
	/// </summary>
	private void MergeComponents(
		OpenApiComponents sourceComponents,
		OpenApiComponents targetComponents,
		string apiName,
		string? virtualPrefix)
	{
		if (sourceComponents?.Schemas is null) return;

		foreach (var (key, schema) in sourceComponents.Schemas)
		{
			if (targetComponents.Schemas.TryAdd(key, schema))
			{
				if (!_schemaOrigins.ContainsKey(key))
					_schemaOrigins[key] = new SchemaOrigin(apiName, virtualPrefix);
			}
		}

		// Merge Security Schemes (preserve downstream scheme definitions)
		// Only merge if no global security scheme is configured
		if (_options.OpenApiSecurityScheme == null && sourceComponents.SecuritySchemes != null)
		{
			foreach (var (key, scheme) in sourceComponents.SecuritySchemes)
				targetComponents.SecuritySchemes.TryAdd(key, scheme);
		}
	}

	/// <summary>
	/// Merges tags prioritizing: Operation Tags > Document Global Tags > URL Fallback
	/// </summary>
	private void MergeTags(
		OpenApiDocument sourceDoc, 
		OpenApiDocument targetDoc, 
		string sourceUrl)
	{
		if (sourceDoc.Tags != null)
		{
			foreach (var tag in sourceDoc.Tags) 
			{
				if (!targetDoc.Tags.Any(t => t.Name == tag.Name)) 
					targetDoc.Tags.Add(tag);
			}
		}

		var defaultTagName = new Uri(sourceUrl).Host.Replace(".", "-");
		foreach (var path in sourceDoc.Paths.Values)
		{
			foreach (var operation in path.Operations.Values)
			{
				operation.Tags ??= new List<OpenApiTag>();
				if (!operation.Tags.Any())
				{
					if (sourceDoc.Tags?.Any() == true)
					{
						foreach (var t in sourceDoc.Tags) 
							operation.Tags.Add(new OpenApiTag { Name = t.Name });
					}						
					else
						operation.Tags.Add(new OpenApiTag { Name = defaultTagName });
				}

				foreach (var tag in operation.Tags)
					if (!targetDoc.Tags.Any(t => t.Name == tag.Name)) 
						targetDoc.Tags.Add(tag);
			}
		}
	}

	/// <summary>
	/// Consolidates the server definitions in the specified OpenAPI document to ensure a valid server entry is present.
	/// </summary>
	private void ConsolidateServerDefinitions(OpenApiDocument doc)
	{
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

	/// <summary>
	/// Adds the configured global security scheme to the specified OpenAPI document and applies it as a global security
	/// requirement if present in the options.
	/// </summary>
	private void ConsolidateGlobalSecurity(OpenApiDocument doc)
	{
		if (_options.OpenApiSecurityScheme is null) return;

		string schemeName = _options.OpenApiSecurityScheme.Name;
		if (string.IsNullOrWhiteSpace(schemeName))
			throw new InvalidOperationException("Security scheme name is required.");

		doc.Components.SecuritySchemes.Clear();
		doc.Components.SecuritySchemes[schemeName] = _options.OpenApiSecurityScheme;

		// Create the global security requirement reference
		var globalSecurityRequirement = new OpenApiSecurityRequirement
		{
			{
				new OpenApiSecurityScheme
				{
					Reference = new OpenApiReference
					{
						Type = ReferenceType.SecurityScheme,
						Id = schemeName
					}
				},
				new List<string>()
			}
		};

		doc.SecurityRequirements.Add(globalSecurityRequirement);
		foreach (var path in doc.Paths.Values)
		{
			foreach (var op in path.Operations.Values)
				op.Security = [globalSecurityRequirement];
		}
		
		_logger.LogInformation("Applied global Gateway security scheme '{SchemeName}'", schemeName);
	}
}