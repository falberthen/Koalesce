using System.IO;

namespace Koalesce.OpenAPI.Base;

/// <summary>
/// Base class for OpenAPI providers, enforcing OpenAPI version validation and serialization.
/// </summary>
/// 
public abstract class OpenApiProviderBase<TOptions> : IKoalesceProvider
	where TOptions : OpenApiOptions
{
	/// <summary>
	/// Provides a serialized OpenAPI document.
	/// </summary>
	/// <returns>A task containing the serialized OpenAPI document.</returns>
	public abstract Task<string> ProvideSerializedDocumentAsync();

	protected readonly IOpenApiDocumentBuilder _openApiDocumentBuilder;
	protected readonly ILogger _logger;
	protected readonly TOptions _providerOptions;

	protected OpenApiProviderBase(
		ILogger logger,
		IOpenApiDocumentBuilder openApiDocumentBuilder,
		IOptions<TOptions> providerOptions)
	{
		ArgumentNullException.ThrowIfNull(logger);
		ArgumentNullException.ThrowIfNull(openApiDocumentBuilder);
		ArgumentNullException.ThrowIfNull(providerOptions?.Value);

		_logger = logger;
		_openApiDocumentBuilder = openApiDocumentBuilder;
		_providerOptions = providerOptions.Value;

		// Enforce OpenAPI version validation at instantiation
		ValidateOpenApiVersion();
	}

	/// <summary>
	/// Serializes an OpenAPI document using the correct OpenAPI spec version and format.
	/// </summary>
	/// <param name="document">The OpenAPI document to serialize.</param>
	/// <returns>A string containing the serialized OpenAPI document.</returns>
	protected string SerializeOpenApiDocument(OpenApiDocument document)
	{
		OpenApiSpecVersion version = GetOpenApiSpecVersion();

		string resultPathExtension = Path
			.GetExtension(_providerOptions.MergedOpenApiPath)
			.ToLowerInvariant();

		// Determine format based on settings
		OpenApiFormat format = resultPathExtension switch
		{
			".yaml" => OpenApiFormat.Yaml,
			_ => OpenApiFormat.Json, // Default to JSON
		};

		// Serialize the document
		string serialized = document.Serialize(version, format);

		// Ensure the correct OpenAPI version is enforced in the output
		serialized = format switch
		{
			OpenApiFormat.Json => serialized.Replace("\"openapi\": \"3.0.4\"", $"\"openapi\": \"{_providerOptions.OpenApiVersion}\""),
			OpenApiFormat.Yaml => serialized.Replace("openapi: 3.0.4", $"openapi: {_providerOptions.OpenApiVersion}"),
			_ => serialized
		};

		_logger.LogInformation("Serialized Koalesced document using version {Version} as {Format}",
			_providerOptions.OpenApiVersion, format);

		return serialized;
	}

	/// <summary>
	/// Determines the corresponding <see cref="OpenApiSpecVersion"/> based on the OpenAPI version.
	/// </summary>
	protected OpenApiSpecVersion GetOpenApiSpecVersion()
	{
		return GetMajorVersion() switch
		{
			"2" => OpenApiSpecVersion.OpenApi2_0,
			"3" => OpenApiSpecVersion.OpenApi3_0,
			_ => throw new NotSupportedException($"Unsupported OpenAPI major version: {_providerOptions.OpenApiVersion}")
		};
	}

	/// <summary>
	/// Extracts and returns the major version from the OpenAPI version string.
	/// </summary>
	/// <returns>The major version as a string.</returns>
	private string GetMajorVersion()
	{
		var version = _providerOptions.OpenApiVersion?.Trim();
		int dotIndex = version.IndexOf('.');
		return dotIndex > 0 ? version[..dotIndex] : version;
	}

	/// <summary>
	/// Validates the OpenAPI version against allowed versions.
	/// </summary>
	private void ValidateOpenApiVersion()
	{
		if (string.IsNullOrWhiteSpace(_providerOptions.OpenApiVersion))
		{
			_logger.LogError("OpenAPI version is missing in configuration.");
			throw new ArgumentException("OpenAPI version cannot be null or empty.", nameof(_providerOptions.OpenApiVersion));
		}

		string majorVersion = GetMajorVersion();

		_logger.LogDebug("Validating OpenAPI version: {Version}", _providerOptions.OpenApiVersion);

		_ = majorVersion switch
		{
			"3" when AllowedOpenApi3Versions.Contains(_providerOptions.OpenApiVersion) => true,
			"2" => true, // Allow all OpenAPI 2.x.x versions dynamically
			_ => throw new NotSupportedException($"Unsupported OpenAPI version: {_providerOptions.OpenApiVersion}")
		};
	}

	private static readonly HashSet<string> AllowedOpenApi3Versions = new()
	{
		OpenApiVersions.V3_0_0,
		OpenApiVersions.V3_0_1,
		OpenApiVersions.V3_0_4,
		OpenApiVersions.V3_1_0
	};

	/// <summary>
	/// Supported OpenAPI Versions
	/// </summary>
	private static class OpenApiVersions
	{
		public const string V3_0_0 = "3.0.0";
		public const string V3_0_1 = "3.0.1";
		public const string V3_0_4 = "3.0.4";
		public const string V3_1_0 = "3.1.0";
	}
}