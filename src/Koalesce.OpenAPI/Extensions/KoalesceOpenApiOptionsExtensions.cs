namespace Koalesce.OpenAPI.Extensions;

public static class KoalesceOpenApiOptionsExtensions
{
	/// <summary>
	/// Configures a global JWT Bearer Token authentication scheme for the merged OpenAPI document.
	/// </summary>
	public static void ApplyGlobalJwtBearerSecurityScheme(this KoalesceOpenApiOptions options,
		string schemeName = OpenAPIConstants.JwtBearerSchemeDefaultName,
		string? description = null)
	{
		options.OpenApiSecurityScheme = new OpenApiSecurityScheme
		{
			Name = schemeName, // Store name here temporarily for the merger to use
			Type = SecuritySchemeType.Http,
			Scheme = "bearer",
			BearerFormat = "JWT",
			Description = description ?? OpenAPIConstants.JwtBearerSchemeDefaultDescription
		};
	}

	/// <summary>
	/// Configures a global API Key authentication scheme for the merged OpenAPI document.
	/// </summary>
	public static void ApplyGlobalApiKeySecurityScheme(this KoalesceOpenApiOptions options,
		string headerName = OpenAPIConstants.ApiKeySchemeDefaultName,
		string? description = null,
		ParameterLocation location = ParameterLocation.Header)
	{
		options.OpenApiSecurityScheme = new OpenApiSecurityScheme
		{
			Name = headerName, // For ApiKey, Name is the header/query/cookie name (correct per spec)
			Type = SecuritySchemeType.ApiKey,
			In = location,
			Description = description ?? OpenAPIConstants.ApiKeySchemeDefaultDescription
		};
	}

	/// <summary>
	/// Configures a global HTTP Basic Authentication scheme for the merged OpenAPI document.
	/// </summary>
	public static void ApplyGlobalBasicAuthSecurityScheme(this KoalesceOpenApiOptions options,
		string schemeName = OpenAPIConstants.BasicAuthSchemeDefaultName,
		string? description = null)
	{
		options.OpenApiSecurityScheme = new OpenApiSecurityScheme
		{
			Name = schemeName, // Store name here temporarily for the merger to use
			Type = SecuritySchemeType.Http,
			Scheme = "basic",
			Description = description ?? OpenAPIConstants.BasicAuthSchemeDefaultDescription
		};
	}

	/// <summary>
	/// Configures a global OAuth2 Client Credentials flow authentication scheme for the merged OpenAPI document.
	/// </summary>
	public static void ApplyGlobalOAuth2ClientCredentialsSecurityScheme(this KoalesceOpenApiOptions options,
		Uri tokenUrl,
		Dictionary<string, string> scopes,
		string schemeName = OpenAPIConstants.OAuth2ClientCredentialsSchemeDefaultName,
		string? description = null)
	{
		options.OpenApiSecurityScheme = new OpenApiSecurityScheme
		{
			Name = schemeName, // Store name here temporarily for the merger to use
			Type = SecuritySchemeType.OAuth2,
			Description = description ?? OpenAPIConstants.OAuth2ClientCredentialsSchemeDefaultDescription,
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
	/// Configures a global OAuth2 Authorization Code flow authentication scheme for the merged OpenAPI document.
	/// </summary>
	public static void ApplyGlobalOAuth2AuthCodeSecurityScheme(this KoalesceOpenApiOptions options,
		Uri authorizationUrl,
		Uri tokenUrl,
		Dictionary<string, string> scopes,
		string schemeName = OpenAPIConstants.OAuth2AuthCodeSchemeDefaultName,
		string? description = null)
	{
		options.OpenApiSecurityScheme = new OpenApiSecurityScheme
		{
			Name = schemeName, // Store name here temporarily for the merger to use
			Type = SecuritySchemeType.OAuth2,
			Description = description ?? OpenAPIConstants.OAuth2AuthCodeSchemeDefaultDescription,
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
	/// Configures a global OpenID Connect (OIDC) authentication scheme for the merged OpenAPI document.
	/// </summary>
	public static void ApplyGlobalOpenIdConnectSecurityScheme(this KoalesceOpenApiOptions options,
		Uri openIdConnectUrl,
		string schemeName = OpenAPIConstants.OpenIdConnectSchemeDefaultName,
		string? description = null)
	{
		options.OpenApiSecurityScheme = new OpenApiSecurityScheme
		{
			Name = schemeName, // Store name here temporarily for the merger to use
			Type = SecuritySchemeType.OpenIdConnect,
			OpenIdConnectUrl = openIdConnectUrl,
			Description = description ?? OpenAPIConstants.OpenIdConnectSchemeDefaultDescription
		};
	}
}