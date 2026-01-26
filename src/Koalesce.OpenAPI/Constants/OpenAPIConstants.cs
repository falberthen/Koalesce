namespace Koalesce.OpenAPI.Constants;

public static class OpenAPIConstants
{
	#region OpenApiOptions Validation Messages
	public const string ApiGatewayBaseUrlValidationError =
		"ApiGatewayBaseUrl must be a valid absolute URL (http:// or https://).";
	#endregion

	#region OpenApiDocumentMerger
	public const string V1 = "v1";
	public const string UnknownApi = "Unknown API";
	#endregion
}
