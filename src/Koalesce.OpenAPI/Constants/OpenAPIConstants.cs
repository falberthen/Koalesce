namespace Koalesce.OpenAPI.Constants;

public static class OpenAPIConstants
{
	#region OpenApiOptions	
	public const string ApiGatewayBaseUrlValidationError =
		"ApiGatewayBaseUrl must be a valid absolute URL (http:// or https://).";
	#endregion

	#region OpenApiDocumentMerger
	public const string V1 = "v1";
	#endregion

	#region OpenApiSecurityExtensions
	public const string JwtBearerSchemeDefaultName = "JWT Bearer";
	public const string ApiKeySchemeDefaultName = "X-Api-Key";
	public const string BasicAuthSchemeDefaultName = "Basic Auth";
	public const string OAuth2ClientCredentialsSchemeDefaultName = "OAuth2 Client Credentials";
	public const string OAuth2AuthCodeSchemeDefaultName = "OAuth2 Code Flow";
	public const string OpenIdConnectSchemeDefaultName = "OpenID Connect";

	public const string JwtBearerSchemeDefaultDescription = "Enter your JWT token in the format: Bearer {token}";
	public const string ApiKeySchemeDefaultDescription = "Enter your API Key here.";
	public const string BasicAuthSchemeDefaultDescription = "Input your username and password";
	public const string OAuth2ClientCredentialsSchemeDefaultDescription = "OAuth2 Authorization Client Credentials";
	public const string OAuth2AuthCodeSchemeDefaultDescription = "OAuth2 Authorization Code Flow";
	public const string OpenIdConnectSchemeDefaultDescription = "OpenID Connect Authentication";
	#endregion
}
