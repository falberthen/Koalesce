namespace Koalesce.Constants;

public static class KoalesceConstants
{
	#region OpenApiOptions Validation Messages
	public const string ApiGatewayBaseUrlValidationError =
		"ApiGatewayBaseUrl must be a valid absolute URL (http:// or https://).";
	#endregion

	#region OpenApiDocumentMerger
	public const string DefaultOpenApiVersion = "3.0.1";
	public const string DefaultTitle = "My Koalesced API";
	public const string DefaultVersion = "v1";
	public const string UnknownApi = "Unknown API";
	public const string UnknownTagName = "Unknown";
	#endregion

	#region OpenAPI Supported Versions
	/// <summary>
	/// Supported OpenAPI specification versions for input and output.
	/// </summary>
	public static readonly HashSet<string> SupportedOpenApiVersions =
	[
		"2.0",
		"3.0.0", "3.0.1", "3.0.2", "3.0.3", "3.0.4",
		"3.1.0", "3.1.1",
		"3.2.0"
	];

	public const string UnsupportedOpenApiVersionError =
		"Unsupported OpenAPI version: {0}. Supported versions: {1}";
	#endregion
}
