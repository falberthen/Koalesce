namespace Koalesce.OpenAPI.Services;

/// <summary>
/// Merges multiple API sources into a single OpenApiDocument
/// </summary>
internal class OpenApiDocumentMerger : IDocumentMerger<OpenApiDocument>
{
	private readonly KoalesceOpenApiOptions _options;
	private readonly ILogger<OpenApiDocumentMerger> _logger;
	private readonly IHttpClientFactory _httpClientFactory;

	/// <summary>
	/// Tracks the origin of each schema for conflict resolution
	/// Key: schema name, Value: origin info (ApiName, VirtualPrefix)
	/// </summary>
	private readonly Dictionary<string, SchemaOrigin> _schemaOrigins = new();

	public OpenApiDocumentMerger(
		IOptions<KoalesceOpenApiOptions> options,
		ILogger<OpenApiDocumentMerger> logger,
		IHttpClientFactory httpClientFactory)
	{
		ArgumentNullException.ThrowIfNull(options);
		ArgumentNullException.ThrowIfNull(logger);
		ArgumentNullException.ThrowIfNull(httpClientFactory);

		_options = options.Value;
		_logger = logger;
		_httpClientFactory = httpClientFactory;
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
			var httpClient = _httpClientFactory.CreateClient(CoreConstants.KoalesceClient);
			var detectedApiVersion = _options.MergedDocumentPath.ExtractVersionFromPath();

			var mergedDocumentDefinition = new OpenApiDocument
			{
				Info = new OpenApiInfo
				{
					Title = _options.Title,
					Version = detectedApiVersion ?? OpenAPIConstants.V1
				},
				Paths = new OpenApiPaths(),
				Components = new OpenApiComponents
				{
					SecuritySchemes = new Dictionary<string, OpenApiSecurityScheme>(),
					Schemas = new Dictionary<string, OpenApiSchema>()
				},
				Servers = new List<OpenApiServer>(),
				// Keep global security empty to prevent scope leakage between microservices
				SecurityRequirements = new List<OpenApiSecurityRequirement>(),
				Tags = new List<OpenApiTag>()
			};

			// Fetch all API definitions concurrently
			// Mapping the tasks to the source definition to pair them later
			var fetchTasks = _options.Sources.Select(async source =>
			{
				var doc = await FetchApiDefinitionAsync(httpClient, source.Url);
				return (ApiSource: source, Document: doc);
			});

			var results = await Task.WhenAll(fetchTasks);

			// Merge results sequentially
			foreach (var (apiSource, downstreamApiDefinition) in results)
			{
				if (downstreamApiDefinition is not null)
				{
					MergeApiDefinition(downstreamApiDefinition, mergedDocumentDefinition, apiSource);
				}
			}

			// Finalize Server definitions (Server Urls, global SecurityScheme)
			ConsolidateServerDefinitions(mergedDocumentDefinition);
			ConsolidateGlobalSecuritySchemeDefinitions(mergedDocumentDefinition);

			_logger.LogInformation("API Koalescing completed.");
			return mergedDocumentDefinition;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error during API Koalescing.");
			throw;
		}
	}

	/// <summary>
	/// Fetches and parses the OpenAPI definition from a URL
	/// </summary>
	private async Task<OpenApiDocument?> FetchApiDefinitionAsync(
		HttpClient httpClient,
		string definitionUrl)
	{
		try
		{
			_logger.LogInformation("Fetching OpenAPI spec from: {Url}", definitionUrl);

			// Using ResponseHeadersRead for efficiency with large streams
			using var response = await httpClient.GetAsync(definitionUrl, HttpCompletionOption.ResponseHeadersRead);
			response.EnsureSuccessStatusCode();

			using var responseStream = await response.Content.ReadAsStreamAsync();
			var readResult = await new OpenApiStreamReader().ReadAsync(responseStream);

			return readResult.OpenApiDocument?.Paths?.Any() == true
				? readResult.OpenApiDocument
				: null;
		}
		catch (Exception ex) when (ex is HttpRequestException || ex is OpenApiException)
		{
			_logger.LogError(ex, "Failed to fetch or parse OpenAPI from {Url}. Skipping.", definitionUrl);
			return null;
		}
	}

	/// <summary>
	/// Merges a specific API definition into the target document, applying prefixes and isolation rules
	/// </summary>
	private void MergeApiDefinition(
		OpenApiDocument sourceDocument,
		OpenApiDocument targetDocument,
		ApiSource apiSource)
	{
		string apiName = sourceDocument.Info?.Title ?? "Unknown API";
		string apiVersion = sourceDocument.Info?.Version ?? "v1";

		// Resolve Schema Conflicts (renaming source schemas if they collide)
		// When both source and existing schema have VirtualPrefix, BOTH are renamed
		OpenApiSchemaConflictResolver
			.ResolveSchemaConflicts(_logger, 
				sourceDocument, 
				targetDocument, 
				apiName, 
				apiSource.VirtualPrefix, 
				_options.SchemaConflictPattern, 
				_schemaOrigins);

		// Prepare the source server entry ONLY for Aggregation Mode.
		// If using Gateway,  ignore source servers here (handled globally in MergeIntoSingleDefinitionAsync).
		OpenApiServer? serverEntry = null;
		if (string.IsNullOrEmpty(_options.ApiGatewayBaseUrl))
		{
			string baseUrl = new Uri(apiSource.Url).GetLeftPart(UriPartial.Authority);
			serverEntry = new OpenApiServer
			{
				Url = baseUrl,
				Description = $"{apiName} ({apiVersion})"
			};

			// Register Server if unique in target (Aggregation Mode requirement)
			if (!targetDocument.Servers.Any(s => s.Url == baseUrl))
				targetDocument.Servers.Add(serverEntry);
		}

		// Merge Core Structures (Paths, Components, Tags)
		MergePaths(
			sourceDocument,
			targetDocument.Paths,
			apiName,
			apiSource,
			serverEntry
		);

		MergeComponents(
			sourceDocument.Components, 
			targetDocument.Components, 
			apiName, 
			apiSource.VirtualPrefix);

		MergeTags(sourceDocument, targetDocument, apiSource.Url);
	}

	/// <summary>
	/// Merges paths, applies route prefixes, and enforces security isolation
	/// </summary>
	private void MergePaths(
		OpenApiDocument sourceDocument,
		OpenApiPaths targetPaths,
		string apiName,
		ApiSource apiSource,
		OpenApiServer? server)
	{
		var sourcePaths = sourceDocument.Paths;
		var globalSecurityRequirements = sourceDocument.SecurityRequirements;
		foreach (var (originalPath, pathItem) in sourcePaths)
		{
			// Check if the path should be excluded
			if (IsPathExcluded(originalPath, apiSource.ExcludePaths))
			{
				_logger.LogInformation("Excluding path '{Path}' from '{ApiName}' as per ExcludePaths configuration.", originalPath, apiName);
				continue;
			}

			// Construct the new path key with optional prefix
			// Logic: "/api/apiname" + Prefix => "/prefix/api/apiname"
			string newPathKey = string.IsNullOrEmpty(apiSource.VirtualPrefix)
				? originalPath
				: $"/{apiSource.VirtualPrefix.Trim('/')}/{originalPath.TrimStart('/')}";

			// Check for collisions
			if (targetPaths.ContainsKey(newPathKey))
			{
				if (!_options.SkipIdenticalPaths)
				{
					throw new KoalesceIdenticalPathFoundException(newPathKey, apiName);
				}
				_logger.LogWarning("Skipping identical path '{Path}' from '{ApiName}'.", newPathKey, apiName);
				continue;
			}

			// Copy path-level metadata and parameters			
			var newPathItem = new OpenApiPathItem
			{
				Summary = pathItem.Summary,
				Description = pathItem.Description,
				Parameters = pathItem.Parameters?.Select(p => p).ToList() ?? new List<OpenApiParameter>()
			};

			targetPaths[newPathKey] = newPathItem;

			foreach (var (opType, operation) in pathItem.Operations)
			{
				// OperationId Namespacing (Critical for API Clients)
				if (!string.IsNullOrEmpty(operation.OperationId) && !string.IsNullOrEmpty(apiSource.VirtualPrefix))
				{
					string cleanPrefix = apiSource.VirtualPrefix.Replace("/", "").Replace("-", "");
					operation.OperationId = $"{cleanPrefix}_{operation.OperationId}";
				}

				// Handle Servers (Gateway vs Direct)
				if (!string.IsNullOrEmpty(_options.ApiGatewayBaseUrl))
				{
					// Gateway handles routing, remove internal servers
					operation.Servers?.Clear();
				}
				else if (server != null)
				{
					operation.Servers ??= new List<OpenApiServer>();

					// Avoid duplication if the server is already listed
					if (!operation.Servers.Any(s => s.Url == server.Url))
					{
						operation.Servers.Add(server);
					}
				}

				// Materialize document-level security requirements into operations
				// Per OpenAPI spec: if operation.Security is null/empty, it inherits from document.Security
				// Need to make this explicit to preserve downstream API security in the merged document
				if ((operation.Security == null || !operation.Security.Any())
					&& globalSecurityRequirements?.Any() == true)
				{
					operation.Security = new List<OpenApiSecurityRequirement>(globalSecurityRequirements);
				}

				operation.Summary ??= string.Empty;
				newPathItem.Operations[opType] = operation;
			}
		}
	}

	/// <summary>
	/// Merges tags prioritizing: Operation Tags > Document Global Tags > URL Fallback
	/// </summary>
	private void MergeTags(
		OpenApiDocument sourceDocument,
		OpenApiDocument targetDocument,
		string sourceUrl)
	{
		// Ensure source global definitions are copied (preserves descriptions)
		if (sourceDocument.Tags is not null)
		{
			foreach (var tag in sourceDocument.Tags)
			{
				if (!targetDocument.Tags.Any(t => t.Name == tag.Name))
					targetDocument.Tags.Add(tag);
			}
		}

		var defaultTagName = new Uri(sourceUrl).Host.Replace(".", "-");

		// Assign tags to operations
		foreach (var path in sourceDocument.Paths.Values)
		{
			foreach (var operation in path.Operations.Values)
			{
				operation.Tags ??= new List<OpenApiTag>();

				if (!operation.Tags.Any())
				{
					// Fallback Strategy:
					// Use Global Source Tags if available
					if (sourceDocument.Tags?.Any() == true)
					{
						foreach (var docTag in sourceDocument.Tags)
							operation.Tags.Add(new OpenApiTag { Name = docTag.Name });
					}
					// Use URL Hostname if no tags exist anywhere
					else
					{
						operation.Tags.Add(new OpenApiTag { Name = defaultTagName });
					}
				}

				// Ensure all operation tags are registered in the target global list
				foreach (var tag in operation.Tags)
				{
					if (!targetDocument.Tags.Any(t => t.Name == tag.Name))
						targetDocument.Tags.Add(tag);
				}
			}
		}
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
		if (sourceComponents is null) return;

		_logger.LogInformation("Merging {Count} schemas...",
			sourceComponents.Schemas?.Count ?? 0);

		// Merge Schemas
		if (sourceComponents.Schemas is not null)
		{
			foreach (var (key, schema) in sourceComponents.Schemas)
			{
				// TryAdd ensures not overwriting existing schemas, but log collisions
				if (targetComponents.Schemas.TryAdd(key, schema))
				{
					// Track the origin of this schema for future conflict resolution
					_schemaOrigins[key] = new SchemaOrigin(apiName, virtualPrefix);
				}
				else
				{
					_logger.LogDebug("Schema '{SchemaKey}' already exists in target. Skipping merge from source to prevent overwrites.", key);
				}
			}
		}

		// Merge Security Schemes (preserve downstream scheme definitions)
		// Only merge if no global security scheme is configured
		if (_options.OpenApiSecurityScheme == null && sourceComponents.SecuritySchemes is not null)
		{
			foreach (var (key, scheme) in sourceComponents.SecuritySchemes)
			{
				if (!targetComponents.SecuritySchemes.TryAdd(key, scheme))
				{
					_logger.LogDebug("Security scheme '{SchemeKey}' already exists in target.", key);
				}
			}
		}
	}

	/// <summary>
	/// Consolidates the server definitions in the specified OpenAPI document to ensure a valid server entry is present.
	/// </summary>
	private void ConsolidateServerDefinitions(OpenApiDocument mergedDocumentDefinition)
	{
		if (!string.IsNullOrEmpty(_options.ApiGatewayBaseUrl))
		{
			// If a Gateway URL is specified, override all server entries
			mergedDocumentDefinition.Servers.Clear();
			mergedDocumentDefinition.Servers.Add(
				new OpenApiServer { Url = _options.ApiGatewayBaseUrl, Description = "API Gateway" }
			);
		}
		else if (!mergedDocumentDefinition.Servers.Any())
		{
			_logger.LogWarning("No valid servers found. Adding fallback '/'");
			mergedDocumentDefinition.Servers.Add(new OpenApiServer { Url = "/" });
		}
	}

	/// <summary>
	/// Adds the configured global security scheme to the specified OpenAPI document and applies it as a global security
	/// requirement if present in the options.
	/// </summary>
	private void ConsolidateGlobalSecuritySchemeDefinitions(OpenApiDocument mergedDocumentDefinition)
	{
		// If OpenApiSecurityScheme is configured, apply it globally
		if (_options.OpenApiSecurityScheme is not null)
		{
			string schemeName = _options.OpenApiSecurityScheme.Name;

			// Validate scheme name is not null or empty
			if (string.IsNullOrWhiteSpace(schemeName))
			{
				_logger.LogError(
					"OpenApiSecurityScheme.Name cannot be null or empty. " +
					"When using extension methods, provide a scheme name. " +
					"When using appsettings.json, ensure the Name property is set.");
				throw new InvalidOperationException(
					"Security scheme name is required. Provide a Name value for the OpenApiSecurityScheme.");
			}

			// Clear all downstream security schemes (we're replacing them with the global scheme)
			mergedDocumentDefinition.Components.SecuritySchemes.Clear();

			// Add the global scheme to components
			mergedDocumentDefinition.Components.SecuritySchemes[schemeName] = _options.OpenApiSecurityScheme;

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

			// Add global security requirement at document level
			mergedDocumentDefinition.SecurityRequirements.Add(globalSecurityRequirement);

			// Apply global security to ALL operations (replacing downstream security)
			foreach (var pathItem in mergedDocumentDefinition.Paths.Values)
			{
				foreach (var operation in pathItem.Operations.Values)
				{
					// Replace operation-level security with the global scheme
					operation.Security = new List<OpenApiSecurityRequirement> { globalSecurityRequirement };
				}
			}

			_logger.LogInformation("Applied global Gateway security scheme '{SchemeName}' to all operations", schemeName);
		}
	}

	/// <summary>
	/// Determines if a path should be excluded based on the ExcludePaths configuration.
	/// Supports exact matches and wildcard patterns (e.g., "/api/admin/*").
	/// </summary>
	private static bool IsPathExcluded(string path, List<string>? excludePaths)
	{
		if (excludePaths == null || excludePaths.Count == 0)
			return false;

		foreach (var pattern in excludePaths)
		{
			if (string.IsNullOrWhiteSpace(pattern))
				continue;

			// Normalize both path and pattern
			string normalizedPath = path.TrimEnd('/');
			string normalizedPattern = pattern.TrimEnd('/');

			// Check for wildcard pattern (e.g., "/api/admin/*")
			if (normalizedPattern.EndsWith("/*"))
			{
				string prefix = normalizedPattern[..^2]; // Remove "/*"
				if (normalizedPath.Equals(prefix, StringComparison.OrdinalIgnoreCase) ||
					normalizedPath.StartsWith(prefix + "/", StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}
			// Exact match
			else if (normalizedPath.Equals(normalizedPattern, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}

		return false;
	}
}