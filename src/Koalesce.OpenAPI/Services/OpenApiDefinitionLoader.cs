namespace Koalesce.OpenAPI.Services;

/// <summary>
/// Service responsible for loading OpenAPI definitions from specified URLs
/// </summary>
internal class OpenApiDefinitionLoader
{
	private readonly IHttpClientFactory _httpClientFactory;
	private readonly ILogger<OpenApiDefinitionLoader> _logger;
	private readonly bool _failOnLoadError;

	public OpenApiDefinitionLoader(
		IHttpClientFactory httpClientFactory,
		ILogger<OpenApiDefinitionLoader> logger,
		IOptions<KoalesceOpenApiOptions> options)
	{
		_httpClientFactory = httpClientFactory;
		_logger = logger;
		_failOnLoadError = options.Value.FailOnServiceLoadError;
	}

	/// <summary>
	/// Asynchronously loads an OpenAPI document from the specified URL.
	/// </summary>	
	/// <exception cref="KoalescePathCouldNotBeLoadedException">
	/// Thrown if the OpenAPI document cannot be loaded or parsed and the loader is configured to fail on load errors.
	/// </exception>
	public async Task<OpenApiDocument?> LoadAsync(string url)
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
			var readResult = await new OpenApiStreamReader().ReadAsync(responseStream);

			if (readResult.OpenApiDocument?.Paths?.Any() != true)
			{
				string msg = $"OpenAPI document at {url} contains no paths or could not be parsed.";

				if (_failOnLoadError)
					throw new KoalescePathCouldNotBeLoadedException(url, msg);

				_logger.LogWarning(msg);
				return null;
			}

			return readResult.OpenApiDocument;
		}
		catch (Exception ex) when (ex is HttpRequestException || ex is OpenApiException || ex is TaskCanceledException)
		{
			if (_failOnLoadError)
			{
				_logger.LogError(ex, "CRITICAL: Failed to load required API source from {Url}.", url);
				throw new KoalescePathCouldNotBeLoadedException(url, "Network or parsing error occurred.", ex);
			}

			_logger.LogError(ex, "Failed to fetch or parse OpenAPI from {Url}. Skipping source.", url);
			return null;
		}
	}
}