namespace Koalesce.OpenAPI.Extensions;

public static class OpenApiSecurityExtensions
{
	/// <summary>
	/// Configures the Gateway to use standard JWT Bearer authentication
	/// This is the most common scenario for modern REST APIs
	/// </summary>
	/// <param name="options">The options builder</param>
	/// <param name="description">Description shown in UI (e.g., Swagger)</param>
	public static void UseJwtBearerGatewaySecurity(this OpenApiOptions options, 
		string description = OpenAPIConstants.JwtBearerSchemeDefaultDescription)
	{
		options.GatewaySecurityScheme = new OpenApiSecurityScheme
		{
			Name = "Authorization",
			Type = SecuritySchemeType.Http,
			Scheme = "bearer",
			BearerFormat = "JWT",
			In = ParameterLocation.Header,
			Description = description
		};
	}

	/// <summary>
	/// Configures the Gateway to use API Key authentication (e.g., X-Api-Key header)
	/// </summary>
	/// <param name="options">The options builder</param>
	/// <param name="headerName">The name of the header key (default: X-Api-Key)</param>
	/// <param name="description">Description shown in UI (e.g., Swagger)</param>
	public static void UseApiKeyGatewaySecurity(this OpenApiOptions options, 
		string headerName = "X-Api-Key", 
		string description = OpenAPIConstants.ApiKeySchemeDefaultDescription)
	{
		options.GatewaySecurityScheme = new OpenApiSecurityScheme
		{
			Name = headerName,
			Type = SecuritySchemeType.ApiKey,
			In = ParameterLocation.Header,
			Description = description
		};
	}

	/// <summary>
	/// Configures the Gateway to use Basic Authentication (Username/Password)
	/// </summary>
	/// <param name="options">The options builder.</param>
	/// <param name="description">Description shown in UI (e.g., Swagger)</param>
	public static void UseBasicAuthGatewaySecurity(this OpenApiOptions options,
		string description = OpenAPIConstants.BasicAuthSchemeDefaultDescription)
	{
		options.GatewaySecurityScheme = new OpenApiSecurityScheme
		{
			Type = SecuritySchemeType.Http,
			Scheme = "basic",
			Description = description
		};
	}

	/// <summary>
	/// Configures the Gateway to use OAuth2 with Client Credentials flow (Machine-to-Machine)
	/// </summary>
	/// <param name="options">The options builder</param>
	/// <param name="tokenUrl">The OAuth2 Token Endpoint URL</param>
	/// <param name="scopes">Dictionary of scopes and descriptions</param>
	/// <param name="description">Description shown in UI (e.g., Swagger)</param>
	public static void UseOAuth2ClientCredentialsGatewaySecurity(this OpenApiOptions options, 
		Uri tokenUrl, Dictionary<string, string> scopes, 
		string description = OpenAPIConstants.OAuth2ClientCredentialsSchemeDefaultDescription)
	{
		options.GatewaySecurityScheme = new OpenApiSecurityScheme
		{
			Type = SecuritySchemeType.OAuth2,
			Description = description,
			Flows = new OpenApiOAuthFlows
			{
				ClientCredentials = new OpenApiOAuthFlow
				{
					TokenUrl = tokenUrl,
					Scopes = scopes
				}
			}
		};
	}

	/// <summary>
	/// Configures the Gateway to use OAuth2 with Authorization Code flow (User-Centric / PKCE)
	/// Commonly used with IdentityServer, Auth0, AzureAD
	/// </summary>
	/// <param name="options">The options builder</param>
	/// <param name="authorizationUrl">The OAuth2 Authorization Endpoint URL</param>
	/// <param name="tokenUrl">The OAuth2 Token Endpoint URL</param>
	/// <param name="scopes">Dictionary of scopes and descriptions</param>
	/// <param name="description">Description shown in UI (e.g., Swagger)</param>
	public static void UseOAuth2AuthCodeGatewaySecurity(this OpenApiOptions options, 
		Uri authorizationUrl, Uri tokenUrl, Dictionary<string, string> scopes, 
		string description = OpenAPIConstants.OAuth2AuthCodeSchemeDefaultDescription)
	{
		options.GatewaySecurityScheme = new OpenApiSecurityScheme
		{
			Type = SecuritySchemeType.OAuth2,
			Description = description,
			Flows = new OpenApiOAuthFlows
			{
				AuthorizationCode = new OpenApiOAuthFlow
				{
					AuthorizationUrl = authorizationUrl,
					TokenUrl = tokenUrl,
					Scopes = scopes
				}
			}
		};
	}

	/// <summary>
	/// Configures the Gateway to use OpenID Connect (OIDC) Discovery
	/// </summary>
	/// <param name="options">The options builder</param>
	/// <param name="openIdConnectUrl">The OIDC Connect URL (e.g. .well-known/openid-configuration).</param>
	/// <param name="description">Description shown in UI (e.g., Swagger)</param>
	public static void UseOpenIdConnectGatewaySecurity(this OpenApiOptions options,
		Uri openIdConnectUrl,
		string description = OpenAPIConstants.OpenIdConnectSchemeDefaultDescription)
	{		
		options.GatewaySecurityScheme = new OpenApiSecurityScheme
		{
			Type = SecuritySchemeType.OpenIdConnect,
			OpenIdConnectUrl = openIdConnectUrl,
			Description = description
		};
	}
}