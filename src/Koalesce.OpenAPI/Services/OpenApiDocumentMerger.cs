namespace Koalesce.OpenAPI.Services;

/// <summary>
/// Merges multiple API sources into a single OpenApiDocument
/// </summary>
public class OpenApiDocumentMerger : IDocumentMerger<OpenApiDocument>
{
	private readonly OpenApiOptions _options;
	private readonly ILogger<OpenApiDocumentMerger> _logger;
	private readonly IHttpClientFactory _httpClientFactory;

	// Regex optimized for performance (compiled) to extract version from paths
	private static readonly Regex VersionRegex = new(@"/(?<version>v\d+)(/|$)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	public OpenApiDocumentMerger(
		IOptions<OpenApiOptions> options,
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

		try
		{
			var httpClient = _httpClientFactory.CreateClient(CoreConstants.KoalesceClient);
			var detectedApiVersion = ExtractVersionFromPath(_options.MergedDocumentPath);

			var mergedDocument = new OpenApiDocument
			{
				Info = new OpenApiInfo
				{
					Title = _options.Title,
					Version = detectedApiVersion ?? "v1"
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

			// Check if we should Unify Security (Gateway Mode)
			bool unifySecurity = !string.IsNullOrEmpty(_options.ApiGatewayBaseUrl)
				&& !_options.IgnoreGatewaySecurity
				&& _options.GatewaySecurityScheme != null;

			if (unifySecurity)
			{
				// Inject Global Gateway Security Scheme
				const string schemeName = OpenAPIConstants.GatewaySecuritySchemeName;
				mergedDocument.Components.SecuritySchemes.Add(schemeName, _options.GatewaySecurityScheme);

				// Apply Global Requirement (all ops inherit this by default)
				mergedDocument.SecurityRequirements.Add(new OpenApiSecurityRequirement
				{
					{
						new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = schemeName } },
						new List<string>()
					}
				});
			}

			// Fetch all API definitions concurrently
			// Mapping the tasks to the source definition to pair them later
			var fetchTasks = _options.Sources.Select(async source =>
			{
				var doc = await FetchApiDefinitionAsync(httpClient, source.Url);
				return (SourceConfig: source, Document: doc);
			});

			var results = await Task.WhenAll(fetchTasks);

			// Merge results sequentially
			foreach (var (sourceConfig, apiDoc) in results)
			{
				if (apiDoc is not null)
				{
					MergeApiDefinition(apiDoc, mergedDocument, sourceConfig, unifySecurity);
				}
			}

			// Finalize Servers based on Gateway Mode vs Aggregation Mode
			if (!string.IsNullOrEmpty(_options.ApiGatewayBaseUrl))
			{
				// Gateway Mode: Single entry point
				mergedDocument.Servers.Clear();
				mergedDocument.Servers.Add(new OpenApiServer { Url = _options.ApiGatewayBaseUrl, Description = "API Gateway" });
			}
			else if (!mergedDocument.Servers.Any())
			{
				// Aggregation Mode Fallback
				_logger.LogWarning("No valid servers found. Adding fallback '/'");
				mergedDocument.Servers.Add(new OpenApiServer { Url = "/" });
			}

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
	/// Fetches and parses the OpenAPI definition from a URL
	/// </summary>
	private async Task<OpenApiDocument?> FetchApiDefinitionAsync(HttpClient httpClient, string definitionUrl)
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
		SourceDefinition sourceConfig,
		bool unifySecurity)
	{
		string apiName = sourceDocument.Info?.Title ?? "Unknown API";
		string apiVersion = sourceDocument.Info?.Version ?? "v1";

		// Prepare the source server entry ONLY for Aggregation Mode.
		// If using Gateway,  ignore source servers here (handled globally in MergeIntoSingleDefinitionAsync).
		OpenApiServer? serverEntry = null;
		if (string.IsNullOrEmpty(_options.ApiGatewayBaseUrl))
		{
			string baseUrl = new Uri(sourceConfig.Url).GetLeftPart(UriPartial.Authority);
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
		// Note: Security Isolation logic is integrated into MergePaths
		MergePaths(
			sourceDocument.Paths,
			targetDocument.Paths,
			serverEntry,
			apiName,
			sourceConfig.VirtualPrefix,
			sourceDocument.SecurityRequirements,
			unifySecurity
		);

		MergeComponents(sourceDocument.Components, targetDocument.Components, unifySecurity);
		MergeTags(sourceDocument, targetDocument, sourceConfig.Url);
	}

	/// <summary>
	/// Merges paths, applies route prefixes, and enforces security isolation
	/// </summary>
	private void MergePaths(
		OpenApiPaths sourcePaths,
		OpenApiPaths targetPaths,
		OpenApiServer? server,
		string apiName,
		string? pathPrefix,
		IList<OpenApiSecurityRequirement>? globalSourceSecurity,
		bool unifySecurity)
	{
		foreach (var (originalPath, pathItem) in sourcePaths)
		{
			// Construct the new path key with optional prefix
			// Logic: "/api/apiname" + Prefix => "/prefix/api/apiname"
			string newPathKey = string.IsNullOrEmpty(pathPrefix)
				? originalPath
				: $"/{pathPrefix.Trim('/')}/{originalPath.TrimStart('/')}";

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
				if (!string.IsNullOrEmpty(operation.OperationId) && !string.IsNullOrEmpty(pathPrefix))
				{
					string cleanPrefix = pathPrefix.Replace("/", "").Replace("-", "");
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

				// Security Logic: Unification vs Isolation
				if (unifySecurity)
				{
					// If unifying, wipe operation security so it inherits the Global Gateway Scheme
					operation.Security?.Clear();
				}
				else
				{
					// Security Isolation
					// If the operation has no specific security, inject the global security from the source.
					// This prevents global security from one API leaking into others in the merged doc.
					if ((operation.Security is null || !operation.Security.Any())
						&& globalSourceSecurity is not null && globalSourceSecurity.Any())
					{
						operation.Security = [.. globalSourceSecurity];
					}
				}

				operation.Summary ??= string.Empty;
				newPathItem.Operations[opType] = operation;
			}
		}
	}

	/// <summary>
	/// Merges tags prioritizing: Operation Tags > Document Global Tags > URL Fallback
	/// </summary>
	private void MergeTags(OpenApiDocument sourceDocument, OpenApiDocument targetDocument, string sourceUrl)
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
	private void MergeComponents(OpenApiComponents sourceComponents, OpenApiComponents targetComponents, bool unifySecurity)
	{
		if (sourceComponents is null) return;

		_logger.LogInformation("Merging {Count} schemas...",
			sourceComponents.Schemas?.Count ?? 0);

		// Merge Schemas
		if (sourceComponents.Schemas is not null)
		{
			foreach (var (key, schema) in sourceComponents.Schemas)
			{
				// TryAdd ensures we don't overwrite existing schemas, but we should log collisions
				if (!targetComponents.Schemas.TryAdd(key, schema))
				{
					_logger.LogDebug("Schema '{SchemaKey}' already exists in target. Skipping merge from source to prevent overwrites.", key);
				}
			}
		}

		// Merge Security Schemes (Definitions only, not requirements)
		// Only merge source schemes if we are NOT unifying security.
		if (!unifySecurity && sourceComponents.SecuritySchemes is not null)
		{
			foreach (var (key, scheme) in sourceComponents.SecuritySchemes)
				targetComponents.SecuritySchemes.TryAdd(key, scheme);
		}
	}

	private string ExtractVersionFromPath(string path)
	{
		if (string.IsNullOrEmpty(path)) return "v1";
		var match = VersionRegex.Match(path);
		return match.Success ? match.Groups["version"].Value : "v1";
	}
}