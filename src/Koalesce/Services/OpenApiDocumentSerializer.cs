namespace Koalesce.Services;

/// <summary>
/// Service responsible for serializing OpenAPI documents to JSON or YAML format.
/// </summary>
internal class OpenApiDocumentSerializer
{
	private readonly ILogger<OpenApiDocumentSerializer> _logger;
	private readonly KoalesceOptions _options;

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
	public string Serialize(OpenApiDocument document, string? outputPath = null)
	{
		OpenApiSpecVersion version = GetOpenApiSpecVersion();
		bool isYaml = IsYamlFormat(outputPath);

		// Serialize the document using async API (run synchronously for interface compatibility)
		string serialized = isYaml
			? document.SerializeAsYamlAsync(version).GetAwaiter().GetResult()
			: document.SerializeAsJsonAsync(version).GetAwaiter().GetResult();

		// Ensure the correct OpenAPI version is enforced in the output
		string pattern = isYaml
			? "openapi:\\s*[^\n]+"
			: "\"openapi\":\\s*\"[^\"]+\"";

		string replacement = isYaml
			? $"openapi: {_options.OpenApiVersion}"
			: $"\"openapi\": \"{_options.OpenApiVersion}\"";

		serialized = Regex.Replace(serialized, pattern, replacement);

		_logger.LogInformation("Serialized Koalesced document using version {Version} as {Format}",
			_options.OpenApiVersion, isYaml ? "YAML" : "JSON");

		return serialized;
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
			!AllowedOpenApiVersions.Contains(_options.OpenApiVersion))
			throw new NotSupportedException($"Unsupported OpenAPI version: {_options.OpenApiVersion}");
	}

	/// <summary>
	/// Allowed OpenAPI versions.
	/// </summary>
	private static readonly HashSet<string> AllowedOpenApiVersions =
	[
		"2.0", "3.0.0", "3.0.1", "3.0.4", "3.1.0"
	];
}
