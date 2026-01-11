namespace Koalesce.OpenAPI;

/// <summary>
/// Options for configuring OpenAPI middleware.
/// </summary>
public class OpenApiOptions : KoalesceOptions
{
	public const string OpenApiVersionDefaultValue = "3.0.1";

	/// <summary>
	/// The OpenAPI specification version
	/// </summary>
	public string OpenApiVersion { get; set; } = OpenApiVersionDefaultValue;
	/// <summary>
	/// The public URL of the API Gateway. 
	/// </summary>
	public string? ApiGatewayBaseUrl { get; set; } = default!;

	/// <summary>
	/// The global security scheme for the Gateway.
	/// </summary>
	public OpenApiSecurityScheme? GatewaySecurityScheme { get; set; }

	/// <summary>
	/// If true, keeps downstream security for each API even if GatewayUrl is set.
	/// </summary>
	public bool IgnoreGatewaySecurity { get; set; } = false;

	public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
	{
		// Core validation
		foreach (var result in base.Validate(validationContext)) yield return result;
		
		bool hasGatewayUrl = !string.IsNullOrWhiteSpace(ApiGatewayBaseUrl);
		if (hasGatewayUrl)
		{
			// Validating URL format
			bool isUriValid = Uri.TryCreate(ApiGatewayBaseUrl, UriKind.Absolute, out Uri? uriResult)
				&& (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
			if (!isUriValid)
			{
				yield return new ValidationResult(
					OpenAPIConstants.ApiGatewayBaseUrlValidationError, 
					[nameof(ApiGatewayBaseUrl)]);
			}

			// Gateway URL requires security scheme unless ignoring security
			if (!IgnoreGatewaySecurity && GatewaySecurityScheme == null)
			{
				yield return new ValidationResult(
					OpenAPIConstants.RequiredGatewaySecuritySchemeValidationError,
					[nameof(GatewaySecurityScheme)]);
			}
		}
	}
}
