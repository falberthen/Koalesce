namespace Koalesce.OpenAPI.Services;

public class OpenApiDocumentSerializer : IMergedDocumentSerializer<OpenApiDocument>
{
	private readonly ILogger<OpenApiDocumentSerializer> _logger;
	private readonly OpenApiOptions _options;

	public OpenApiDocumentSerializer(
		ILogger<OpenApiDocumentSerializer> logger,
		IOptions<OpenApiOptions> options)
	{
		ArgumentNullException.ThrowIfNull(logger);
		ArgumentNullException.ThrowIfNull(options?.Value);

		_logger = logger;
		_options = options.Value;
		ValidateOpenApiVersion();
	}

	/// <inheritdoc/>
	public string Serialize(OpenApiDocument document)
	{
		OpenApiSpecVersion version = GetOpenApiSpecVersion();
		OpenApiFormat format = GetFormatFromPath();

		// Serialize the document
		string serialized = document.Serialize(version, format);

		// Ensure the correct OpenAPI version is enforced in the output
		string pattern = format == OpenApiFormat.Json
			? "\"openapi\":\\s*\"[^\"]+\""
			: "openapi:\\s*[^\n]+";

		string replacement = format == OpenApiFormat.Json
			? $"\"openapi\": \"{_options.OpenApiVersion}\""
			: $"openapi: {_options.OpenApiVersion}";

		serialized = Regex.Replace(serialized, pattern, replacement);

		_logger.LogInformation("Serialized Koalesced document using version {Version} as {Format}",
			_options.OpenApiVersion, format);

		return serialized;
	}

	/// <summary>
	/// Determines the OpenAPI format from the file extension.
	/// </summary>
	private OpenApiFormat GetFormatFromPath() =>
		Path.GetExtension(_options.MergedOpenApiPath).ToLowerInvariant() switch
		{
			".yaml" => OpenApiFormat.Yaml,
			_ => OpenApiFormat.Json, // Default to JSON
		};

	/// <summary>
	/// Determines the OpenAPI spec version.
	/// </summary>
	private OpenApiSpecVersion GetOpenApiSpecVersion() =>
		GetMajorVersion() switch
		{
			"2" => OpenApiSpecVersion.OpenApi2_0,
			"3" => OpenApiSpecVersion.OpenApi3_0,
			_ => throw new NotSupportedException($"Unsupported OpenAPI version: {_options.OpenApiVersion}")
		};

	/// <summary>
	/// Extracts the major OpenAPI version.
	/// </summary>
	private string GetMajorVersion() =>	
		_options.OpenApiVersion?.Trim().Split('.')[0] 
			?? throw new ArgumentException("Invalid OpenAPI version");

	/// <summary>
	/// Validates the OpenAPI version.
	/// </summary>
	private void ValidateOpenApiVersion()
	{
		if (string.IsNullOrWhiteSpace(_options.OpenApiVersion) ||
			!AllowedOpenApiVersions.Contains(_options.OpenApiVersion))
		{
			throw new NotSupportedException($"Unsupported OpenAPI version: {_options.OpenApiVersion}");
		}
	}

	/// <summary>
	/// Allowed OpenAPI versions.
	/// </summary>
	private static readonly HashSet<string> AllowedOpenApiVersions = new()
	{
		"3.0.0", "3.0.1", "3.0.4", "3.1.0", "2.0"
	};
}
