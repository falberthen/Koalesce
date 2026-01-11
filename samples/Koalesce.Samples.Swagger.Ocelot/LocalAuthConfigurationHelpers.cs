using Koalesce.OpenAPI;
using Koalesce.OpenAPI.Extensions;

namespace Koalesce.Samples.Swagger.Ocelot;

/// <summary>
/// These methods demonstrate how a user would configure specific scenarios
/// </summary>
public class LocalAuthConfigurationHelpers
{
	public static void ConfigureJwtScenario(OpenApiOptions options) =>		
		options.UseJwtBearerGatewaySecurity();

	public static void ConfigureApiKeyScenario(OpenApiOptions options) =>	
		options.UseApiKeyGatewaySecurity(
			headerName: "X-API-KEY"
		);	

	public static void ConfigureBasicAuthScenario(OpenApiOptions options) =>
		options.UseBasicAuthGatewaySecurity();

	public static void ConfigureOAuth2ClientCredentialsGatewaySecurity(OpenApiOptions options) =>
		options.UseOAuth2ClientCredentialsGatewaySecurity(
			tokenUrl: new Uri("https://localhost:5001/connect/token"),
			scopes: new Dictionary<string, string>
			{
				{ "my_api.read", "Read Access" },
				{ "my_api.write", "Write Access" }
			}
		);

	public static void ConfigureOAuth2AuthCodeScenario(OpenApiOptions options) =>
		options.UseOAuth2AuthCodeGatewaySecurity(
			authorizationUrl: new Uri("https://localhost:5001/connect/authorize"),
			tokenUrl: new Uri("https://localhost:5001/connect/token"),
			scopes: new Dictionary<string, string>
			{
				{ "openid", "OpenID Connect" },
				{ "profile", "User Profile" },
				{ "my_api.full_access", "Full API Access" }
			}
		);

	public static void ConfigureOpenIdConnectScenario(OpenApiOptions options) =>
		options.UseOpenIdConnectGatewaySecurity(
			openIdConnectUrl: new Uri("https://localhost:5001/.well-known/openid-configuration")
		);
}
