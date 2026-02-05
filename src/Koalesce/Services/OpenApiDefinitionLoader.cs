namespace Koalesce.Services;

/// <summary>
/// Service responsible for loading OpenAPI definitions from URLs or local files.
/// </summary>
internal class OpenApiDefinitionLoader
{
	private readonly IHttpClientFactory _httpClientFactory;
	private readonly ILogger<OpenApiDefinitionLoader> _logger;
	private readonly bool _failOnLoadError;
	private readonly OpenApiReaderSettings _readerSettings;

	// Regex patterns for extracting OpenAPI/Swagger version from document content
	private static readonly Regex JsonOpenApiVersionRegex = new(@"""openapi""\s*:\s*""([^""]+)""", RegexOptions.Compiled);
	private static readonly Regex YamlOpenApiVersionRegex = new(@"^openapi:\s*['""]?(\d+\.\d+\.\d+)['""]?", RegexOptions.Multiline | RegexOptions.Compiled);
	private static readonly Regex JsonSwaggerVersionRegex = new(@"""swagger""\s*:\s*""([^""]+)""", RegexOptions.Compiled);
	private static readonly Regex YamlSwaggerVersionRegex = new(@"^swagger:\s*['""]?(\d+\.\d+)['""]?", RegexOptions.Multiline | RegexOptions.Compiled);

	public OpenApiDefinitionLoader(
		IHttpClientFactory httpClientFactory,
		ILogger<OpenApiDefinitionLoader> logger,
		IOptions<KoalesceOptions> options)
	{
		_httpClientFactory = httpClientFactory;
		_logger = logger;
		_failOnLoadError = options.Value.FailOnServiceLoadError;

		// Configure reader settings with YAML support
		_readerSettings = new OpenApiReaderSettings();
		_readerSettings.AddYamlReader();
	}

	/// <summary>
	/// Asynchronously loads an OpenAPI document from the specified source (URL or file).
	/// </summary>
	public async Task<OpenApiDocument?> LoadAsync(ApiSource source)
	{
		bool isFileBased = !string.IsNullOrWhiteSpace(source.FilePath);
		string sourceLocation = isFileBased ? source.FilePath! : source.Url!;

		return isFileBased
			? await LoadFromFileAsync(sourceLocation)
			: await LoadFromHttpAsync(sourceLocation);
	}

	/// <summary>
	/// Asynchronously loads an OpenAPI document from the specified HTTP URL.
	/// </summary>	
	private async Task<OpenApiDocument?> LoadFromHttpAsync(string url)
	{
		try
		{
			_logger.LogInformation("Fetching OpenAPI spec from: {Url}", url);

			var httpClient = _httpClientFactory.CreateClient(CoreConstants.KoalesceClient);
			using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);

			if (_failOnLoadError)
			{
				response.EnsureSuccessStatusCode();
			}
			else if (!response.IsSuccessStatusCode)
			{
				_logger.LogWarning("Failed to fetch OpenAPI from {Url}. Status Code: {StatusCode}. Skipping.", url, response.StatusCode);
				return null;
			}

			using var responseStream = await response.Content.ReadAsStreamAsync();
			return await ParseOpenApiStreamAsync(responseStream, url);
		}
		catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
		{
			return HandleLoadException(ex, url);
		}
	}

	/// <summary>
	/// Asynchronously loads an OpenAPI document from the specified file path.
	/// </summary>	
	private async Task<OpenApiDocument?> LoadFromFileAsync(string filePath)
	{
		try
		{
			string resolvedPath = Path.IsPathRooted(filePath)
				? filePath
				: Path.Combine(AppContext.BaseDirectory, filePath);

			_logger.LogInformation("Loading OpenAPI spec from file: {FilePath}", resolvedPath);

			using var fileStream = File.OpenRead(resolvedPath);
			return await ParseOpenApiStreamAsync(fileStream, resolvedPath);
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			return HandleLoadException(ex, filePath);
		}
	}

	/// <summary>
	/// Asynchronously parses an OpenAPI document from the specified stream and source location.
	/// </summary>
	private async Task<OpenApiDocument?> ParseOpenApiStreamAsync(Stream stream, string sourceLocation)
	{
		try
		{
			// Read stream content to validate version before parsing
			using var reader = new StreamReader(stream, leaveOpen: true);
			string content = await reader.ReadToEndAsync();

			// Validate OpenAPI version
			string? version = ExtractOpenApiVersion(content);
			if (!ValidateSourceVersion(version, sourceLocation))
				return null;

			// Reset stream position and parse
			using var contentStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
			string? format = sourceLocation.GetFormatFromLocation();
			var readResult = await OpenApiDocument.LoadAsync(contentStream, format, _readerSettings);

			if (readResult.Document?.Paths?.Count == 0)
			{
				string msg = $"OpenAPI document at {sourceLocation} contains no paths or could not be parsed.";

				if (_failOnLoadError)
					throw new KoalescePathCouldNotBeLoadedException(sourceLocation, msg);

				_logger.LogWarning("{Message}", msg);
				return null;
			}

			// Log any diagnostics
			if (readResult.Diagnostic?.Errors?.Count > 0)
			{
				foreach (var error in readResult.Diagnostic.Errors)
					_logger.LogWarning("OpenAPI parsing warning at {Location}: {Message}", sourceLocation, error.Message);
			}

			return readResult.Document;
		}
		catch (OpenApiReaderException ex)
		{
			return HandleLoadException(ex, sourceLocation);
		}
	}

	/// <summary>
	/// Extracts the OpenAPI or Swagger version from document content.
	/// </summary>
	private static string? ExtractOpenApiVersion(string content)
	{
		// Try OpenAPI 3.x (JSON)
		var match = JsonOpenApiVersionRegex.Match(content);
		if (match.Success)
			return match.Groups[1].Value;

		// Try OpenAPI 3.x (YAML)
		match = YamlOpenApiVersionRegex.Match(content);
		if (match.Success)
			return match.Groups[1].Value;

		// Try Swagger 2.0 (JSON)
		match = JsonSwaggerVersionRegex.Match(content);
		if (match.Success)
			return match.Groups[1].Value;

		// Try Swagger 2.0 (YAML)
		match = YamlSwaggerVersionRegex.Match(content);
		if (match.Success)
			return match.Groups[1].Value;

		return null;
	}

	/// <summary>
	/// Validates that the source document version is supported.
	/// </summary>
	private bool ValidateSourceVersion(string? version, string sourceLocation)
	{
		if (string.IsNullOrWhiteSpace(version))
		{
			string msg = $"Could not determine OpenAPI version from document at {sourceLocation}.";

			if (_failOnLoadError)
				throw new KoalescePathCouldNotBeLoadedException(sourceLocation, msg);

			_logger.LogWarning("{Message} Skipping.", msg);
			return false;
		}

		if (!KoalesceConstants.SupportedOpenApiVersions.Contains(version))
		{
			var supported = string.Join(", ", KoalesceConstants.SupportedOpenApiVersions);
			string msg = string.Format(KoalesceConstants.UnsupportedOpenApiVersionError, version, supported);

			if (_failOnLoadError)
				throw new KoalescePathCouldNotBeLoadedException(sourceLocation, msg);

			_logger.LogWarning("Source at {Location}: {Message} Skipping.", sourceLocation, msg);
			return false;
		}

		_logger.LogDebug("Source at {Location} has OpenAPI version {Version}.", sourceLocation, version);
		return true;
	}

	/// <summary>
	/// Handles exceptions that occur during the loading or parsing of an OpenAPI document from a specified source
	/// location.
	/// </summary>	
	private OpenApiDocument? HandleLoadException(Exception ex, string sourceLocation)
	{
		if (_failOnLoadError)
		{
			_logger.LogError(ex, "CRITICAL: Failed to load required API source from {Source}.", sourceLocation);
			throw new KoalescePathCouldNotBeLoadedException(sourceLocation, "Failed to load or parse OpenAPI document.", ex);
		}

		_logger.LogError(ex, "Failed to load or parse OpenAPI from {Source}. Skipping source.", sourceLocation);
		return null;
	}
}