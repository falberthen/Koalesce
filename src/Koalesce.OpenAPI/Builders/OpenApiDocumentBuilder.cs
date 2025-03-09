namespace Koalesce.OpenAPI.Builders;

/// <summary>
/// Builds a single API definition document from multiple OpenAPI sources.
/// </summary>
public class OpenApiDocumentBuilder<TOptions> : IOpenApiDocumentBuilder
	where TOptions : OpenApiOptions
{
	private readonly TOptions _options;
	private readonly ILogger<OpenApiDocumentBuilder<TOptions>> _logger;
	private readonly IHttpClientFactory _httpClientFactory;

	public OpenApiDocumentBuilder(
		IOptions<TOptions> options,
		ILogger<OpenApiDocumentBuilder<TOptions>> logger,
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
	public async Task<OpenApiDocument> BuildSingleDefinitionAsync(IEnumerable<string> definitionUrls)
	{
		if (!definitionUrls.Any())
			throw new ArgumentException("API URL list cannot be empty.");

		_logger.LogInformation("Starting API Koalescing process with {SourceDefinitionsCount} APIs",
			definitionUrls.Count());

		var httpClient = _httpClientFactory.CreateClient();
		var detectedApiVersion = ExtractVersionFromPath(_options.MergedOpenApiPath);

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
				SecuritySchemes = new Dictionary<string, OpenApiSecurityScheme>()
			},
			Servers = new List<OpenApiServer>(),
			SecurityRequirements = new List<OpenApiSecurityRequirement>()
		};

		// Fetch all API definitions concurrently
		var fetchResults = await Task.WhenAll(definitionUrls.Select(url =>
			FetchApiDefinitionAsync(httpClient, url))
		);

		// Merge results sequentially in the order of definitionUrls
		foreach (var (url, apiDoc) in definitionUrls.Zip(fetchResults))
		{
			if (apiDoc != null)
				MergeApiDefinition(apiDoc, mergedDocument, url);
		}

		// Ensure at least one fallback server exists
		if (!mergedDocument.Servers.Any())
		{
			_logger.LogWarning("No valid servers found in API definitions. Adding fallback '/'");
			mergedDocument.Servers.Add(new OpenApiServer { Url = "/" });
		}

		_logger.LogInformation("API Koalescing completed.");
		return mergedDocument;
	}

	/// <summary>
	/// Fetches and process API definition provided in SourceOpenApiUrls
	/// </summary>
	private async Task<OpenApiDocument?> FetchApiDefinitionAsync(HttpClient httpClient, string definitionUrl)
	{
		try
		{
			_logger.LogInformation("Fetching OpenAPI spec from: {Url}", definitionUrl);
			using var responseStream = await httpClient.GetStreamAsync(definitionUrl);

			var readResult = await new OpenApiStreamReader().ReadAsync(responseStream);
			return readResult.OpenApiDocument?.Paths.Any() == true
				? readResult.OpenApiDocument
				: null;
		}
		catch (Exception ex) when (ex is HttpRequestException || ex is OpenApiException)
		{
			_logger.LogError(ex,
				"Failed to fetch or parse OpenAPI from {Url}. Skipping.", definitionUrl);
			return null;
		}
	}

	/// <summary>
	/// Merges an API definition into the target merged document.
	/// </summary>    
	private void MergeApiDefinition(OpenApiDocument sourceDocument, OpenApiDocument targetDocument, string definitionUrl)
	{
		string apiName = sourceDocument.Info?.Title ?? "Unknown API";
		string apiVersion = sourceDocument.Info?.Version ?? "v1";		
		string baseUrl = _options.ApiGatewayBaseUrl 
			?? new Uri(definitionUrl).GetLeftPart(UriPartial.Authority);

		var serverEntry = new OpenApiServer
		{
			Url = baseUrl,
			Description = string.IsNullOrEmpty(_options.ApiGatewayBaseUrl)
				? $"{apiName} ({apiVersion})"
				: string.Empty
		};

		if (!targetDocument.Servers.Any(s => s.Url == baseUrl))
			targetDocument.Servers.Add(serverEntry);

		MergePaths(sourceDocument.Paths, targetDocument.Paths, serverEntry);
		MergeComponents(sourceDocument.Components, targetDocument.Components);
		MergeSecuritySchemes(sourceDocument, targetDocument);
		MergeTags(sourceDocument, targetDocument, serverEntry);
	}

	/// <summary>
	/// Merges API paths while preserving per-operation security and summaries.
	/// </summary>
	private void MergePaths(OpenApiPaths sourcePaths, OpenApiPaths targetPaths, OpenApiServer server)
	{
		foreach (var (pathKey, pathItem) in sourcePaths)
		{
			if (!targetPaths.TryGetValue(pathKey, out var existingPath))
				targetPaths[pathKey] = existingPath = new OpenApiPathItem();

			foreach (var (operationType, operation) in pathItem.Operations)
			{
				if (!existingPath.Operations.ContainsKey(operationType))
				{
					// If an API Gateway is used, remove per-path servers to ensure all requests go through the gateway
					if (!string.IsNullOrEmpty(_options.ApiGatewayBaseUrl))
					{
						// Avoids redundant servers, as the gateway handles routing
						operation.Servers?.Clear();
					}
					else
					{
						// Use the specific API server if no gateway is defined
						operation.Servers ??= new List<OpenApiServer>();
						operation.Servers.Add(server);
					}

					operation.Summary ??= string.Empty;
					existingPath.Operations[operationType] = operation;
				}
			}
		}
	}

	/// <summary>
	/// Merges API tags while preserving existing ones.
	/// </summary>
	private void MergeTags(OpenApiDocument sourceDocument, OpenApiDocument targetDocument, OpenApiServer serverEntry)
	{
		targetDocument.Tags ??= new List<OpenApiTag>();

		string defaultTagName = new Uri(serverEntry.Url).Host.Replace(".", "-");
		var defaultTag = new OpenApiTag { Name = defaultTagName };

		foreach (var path in sourceDocument.Paths.Values)
		{
			foreach (var operation in path.Operations.Values)
			{
				if (operation.Tags == null || !operation.Tags.Any())
				{
					operation.Tags = new List<OpenApiTag> { defaultTag };
				}

				foreach (var tag in operation.Tags)
				{
					if (targetDocument.Tags.All(t => t.Name != tag.Name))
					{
						targetDocument.Tags.Add(tag);
					}
				}
			}
		}
	}

	/// <summary>
	/// Merges components (schemas).
	/// </summary>
	private void MergeComponents(OpenApiComponents sourceComponents, OpenApiComponents targetComponents)
	{
		if (sourceComponents?.Schemas == null) return;

		_logger.LogInformation("Merging {Count} component schemas...",
			sourceComponents.Schemas.Count);

		foreach (var (key, schema) in sourceComponents.Schemas)
			targetComponents.Schemas.TryAdd(key, schema);
	}

	/// <summary>
	/// Merges security schemes and security requirements.
	/// </summary>
	private void MergeSecuritySchemes(OpenApiDocument sourceDocument, OpenApiDocument targetDocument)
	{
		if (sourceDocument.Components?.SecuritySchemes != null)
		{
			_logger.LogInformation("Merging {Count} security schemes...",
				sourceDocument.Components.SecuritySchemes.Count);

			foreach (var securityScheme in sourceDocument.Components.SecuritySchemes)
			{
				if (!targetDocument.Components.SecuritySchemes.ContainsKey(securityScheme.Key))
					targetDocument.Components.SecuritySchemes[securityScheme.Key] = securityScheme.Value;
			}
		}

		if (sourceDocument.SecurityRequirements != null)
		{
			foreach (var securityRequirement in sourceDocument.SecurityRequirements)
			{
				if (!targetDocument.SecurityRequirements.Contains(securityRequirement))
					targetDocument.SecurityRequirements.Add(securityRequirement);
			}
		}
	}

	/// <summary>
	/// Extracts a version from the MergedOpenApiPath if provided.
	/// </summary>
	private string ExtractVersionFromPath(string path)
	{
		var match = Regex.Match(path, @"/(?<version>v\d+)(/|$)", RegexOptions.IgnoreCase);
		return match.Success ? match.Groups["version"].Value : "v1";
	}
}