namespace Koalesce.OpenAPI.Services;

/// <summary>
/// Service responsible for serializing OpenAPI documents to JSON or YAML format.
/// </summary>
internal class OpenApiDocumentSerializer : IMergedDocumentSerializer<OpenApiDocument>
{
	private readonly ILogger<OpenApiDocumentSerializer> _logger;
	private readonly KoalesceOpenApiOptions _options;

	public OpenApiDocumentSerializer(
		ILogger<OpenApiDocumentSerializer> logger,
		IOptions<KoalesceOpenApiOptions> options)
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
		bool isYaml = IsYamlFormat();

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
	/// Determines if the output format should be YAML.
	/// </summary>
	private bool IsYamlFormat() =>
		Path.GetExtension(_options.MergedDocumentPath).ToLowerInvariant() switch
		{
			".yaml" or ".yml" => true,
			_ => false
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
