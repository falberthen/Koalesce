using Koalesce.OpenAPI.Extensions;
using Koalesce.OpenAPI.Options;

namespace Koalesce.Samples.Swagger.Ocelot;

/// <summary>
/// These methods demonstrate how a user would configure specific scenarios
/// </summary>
public class LocalAuthConfigurationHelpers
{
	public static void ConfigureJwtScenario(KoalesceOpenApiOptions options) =>
		options.ApplyGlobalJwtBearerSecurityScheme();

	public static void ConfigureApiKeyScenario(KoalesceOpenApiOptions options) =>
		options.ApplyGlobalApiKeySecurityScheme();

	public static void ConfigureBasicAuthScenario(KoalesceOpenApiOptions options) =>
		options.ApplyGlobalBasicAuthSecurityScheme();

	public static void ConfigureOAuth2ClientCredentialsGatewaySecurity(KoalesceOpenApiOptions options) =>
		options.ApplyGlobalOAuth2ClientCredentialsSecurityScheme(
			tokenUrl: new Uri("https://localhost:5001/connect/token"),
			scopes: new Dictionary<string, string>
			{
				{ "my_api.read", "Read Access" },
				{ "my_api.write", "Write Access" }
			}
		);

	public static void ConfigureOAuth2AuthCodeScenario(KoalesceOpenApiOptions options) =>
		options.ApplyGlobalOAuth2AuthCodeSecurityScheme(
			authorizationUrl: new Uri("https://localhost:5001/connect/authorize"),
			tokenUrl: new Uri("https://localhost:5001/connect/token"),
			scopes: new Dictionary<string, string>
			{
				{ "openid", "OpenID Connect" },
				{ "profile", "User Profile" },
				{ "my_api.full_access", "Full API Access" }
			}
		);

	public static void ConfigureOpenIdConnectScenario(KoalesceOpenApiOptions options) =>
		options.ApplyGlobalOpenIdConnectSecurityScheme(
			openIdConnectUrl: new Uri("https://localhost:5001/.well-known/openid-configuration")
		);
}
