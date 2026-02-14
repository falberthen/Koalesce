namespace Koalesce.Services;

/// <summary>
/// Service responsible for serializing OpenAPI documents to JSON or YAML format.
/// </summary>
internal class OpenApiDocumentSerializer
{
	private readonly ILogger<OpenApiDocumentSerializer> _logger;
	private readonly KoalesceOptions _options;

	// Compiled regex patterns for better performance
	private static readonly Regex YamlVersionPattern = new(@"^openapi:\s*.+$", RegexOptions.Compiled | RegexOptions.Multiline);
	private static readonly Regex JsonVersionPattern = new(@"""openapi""\s*:\s*""[^""]+""", RegexOptions.Compiled);

	public OpenApiDocumentSerializer(
		ILogger<OpenApiDocumentSerializer> logger,
		IOptions<KoalesceOptions> options)
	{
		ArgumentNullException.ThrowIfNull(logger);
		ArgumentNullException.ThrowIfNull(options?.Value);

		_logger = logger;
		_options = options.Value;
		ValidateOpenApiVersion();
	}

	/// <summary>
	/// Serializes the OpenAPI document to JSON or YAML format.
	/// </summary>
	/// <param name="document">The OpenAPI document to serialize.</param>
	/// <param name="outputPath">Optional output path to determine format. Falls back to MergedEndpoint if not provided.</param>
	public async Task<string> SerializeAsync(OpenApiDocument document, string? outputPath = null)
	{
		OpenApiSpecVersion version = GetOpenApiSpecVersion();
		bool isYaml = IsYamlFormat(outputPath);

		string serialized = isYaml
			? await document.SerializeAsYamlAsync(version)
			: await document.SerializeAsJsonAsync(version);

		// Ensure the correct OpenAPI version is enforced in the output
		serialized = ReplaceOpenApiVersion(serialized, isYaml);

		_logger.LogInformation("Serialized Koalesced document using version {Version} as {Format}",
			_options.OpenApiVersion, isYaml ? "YAML" : "JSON");

		return serialized;
	}

	/// <summary>
	/// Replaces the OpenAPI version in the serialized document with the configured version.
	/// Uses compiled regex patterns for better performance and only replaces the first occurrence.
	/// </summary>
	private string ReplaceOpenApiVersion(string serialized, bool isYaml)
	{
		if (isYaml)
			return YamlVersionPattern.Replace(serialized, $"openapi: {_options.OpenApiVersion}", count: 1);

		return JsonVersionPattern.Replace(serialized, $"\"openapi\": \"{_options.OpenApiVersion}\"", count: 1);
	}

	/// <summary>
	/// Determines if the output format should be YAML based on the path extension.
	/// </summary>
	/// <param name="outputPath">Optional output path. Falls back to MergedEndpoint if not provided.</param>
	private bool IsYamlFormat(string? outputPath = null)
	{
		// Use outputPath if provided, otherwise fall back to MergedEndpoint
		var path = outputPath ?? _options.MergedEndpoint;
		return Path.GetExtension(path)?.ToLowerInvariant() switch
		{
			".yaml" or ".yml" => true,
			_ => false
		};
	}

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
			!KoalesceConstants.SupportedOpenApiVersions.Contains(_options.OpenApiVersion))
		{
			var supported = string.Join(", ", KoalesceConstants.SupportedOpenApiVersions);
			throw new NotSupportedException(
				string.Format(KoalesceConstants.UnsupportedOpenApiVersionError, _options.OpenApiVersion, supported));
		}
	}
}
