namespace Koalesce.OpenAPI.Options;

/// <summary>
/// Options for configuring OpenAPI middleware
/// </summary>
public class KoalesceOpenApiOptions : KoalesceOptions
{
	public const string OpenApiVersionDefaultValue = "3.0.1";

	/// <summary>
	/// The OpenAPI specification version
	/// </summary>
	public string OpenApiVersion { get; set; } = OpenApiVersionDefaultValue;
	/// <summary>
	/// The public URL of the API Gateway
	/// </summary>
	public string? ApiGatewayBaseUrl { get; set; } = default!;

	/// <summary>
	/// Optional global security scheme for the merged document
	/// When configured, this scheme is added to the merged document and applied globally to all operations
	/// </summary>
	public OpenApiSecurityScheme? OpenApiSecurityScheme { get; set; }

	/// <summary>
	/// Pattern for resolving schema name conflicts
	/// Available placeholders: {Prefix}, {SchemaName}
	/// Default: "{Prefix}_{SchemaName}"
	/// </summary>
	public string SchemaConflictPattern { get; set; } = "{Prefix}_{SchemaName}";

	public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
	{
		// Core validation
		foreach (var result in base.Validate(validationContext))
			yield return result;

		// Validate Gateway URL format if provided
		if (!string.IsNullOrWhiteSpace(ApiGatewayBaseUrl))
		{
			bool isUriValid = Uri.TryCreate(ApiGatewayBaseUrl, UriKind.Absolute, out Uri? uriResult)
				&& (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

			if (!isUriValid)
			{
				yield return new ValidationResult(
					OpenAPIConstants.ApiGatewayBaseUrlValidationError,
					[nameof(ApiGatewayBaseUrl)]);
			}
		}

		// Validate SchemaConflictPattern contains required placeholders
		if (!string.IsNullOrWhiteSpace(SchemaConflictPattern))
		{
			bool hasPrefix = SchemaConflictPattern.Contains("{Prefix}");
			bool hasSchemaName = SchemaConflictPattern.Contains("{SchemaName}");

			if (!hasPrefix || !hasSchemaName)
			{
				yield return new ValidationResult(
					OpenAPIConstants.SchemaConflictPatternValidationError,
					[nameof(SchemaConflictPattern)]);
			}
		}
	}
}
