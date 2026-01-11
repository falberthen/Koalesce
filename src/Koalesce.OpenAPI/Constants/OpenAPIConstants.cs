namespace Koalesce.OpenAPI.Constants;

public static class OpenAPIConstants
{
	#region OpenApiOptions
	public const string RequiredGatewaySecuritySchemeValidationError =
		"GatewaySecurityScheme is required when ApiGatewayBaseUrl is set and security is not ignored.";
	public const string ApiGatewayBaseUrlValidationError = "ApiGatewayBaseUrl must be a valid absolute URL.";
	#endregion

	#region OpenApiDocumentMerger
	public const string GatewaySecuritySchemeName = "GatewaySecurity";
	#endregion

	#region OpenApiSecurityExtensions
	public const string JwtBearerSchemeDefaultDescription = "Enter your JWT token in the format: Bearer {token}";
	public const string ApiKeySchemeDefaultDescription = "Enter your API Key here.";
	public const string BasicAuthSchemeDefaultDescription = "Input your username and password";
	public const string OAuth2ClientCredentialsSchemeDefaultDescription = "OAuth2 Authorization Client Credentials";
	public const string OAuth2AuthCodeSchemeDefaultDescription = "OAuth2 Authorization Code Flow";
	public const string OpenIdConnectSchemeDefaultDescription = "OpenID Connect Authentication";
	#endregion
}
