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
	}
}
