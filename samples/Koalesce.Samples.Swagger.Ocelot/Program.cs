using Koalesce.Core.Extensions;
using Koalesce.OpenAPI.Extensions;
using Koalesce.Samples.Swagger.Ocelot;
using Microsoft.Extensions.Options;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
IServiceCollection services = builder.Services;

// Load merged ocelot.json
builder.Configuration
	.SetBasePath(Directory.GetCurrentDirectory())
	.AddJsonFile("Ocelot/ocelot.json", optional: false, reloadOnChange: true)
	.AddOcelot(
		folder: "Ocelot",
		env: builder.Environment,
		mergeTo: MergeOcelotJson.ToFile,
		primaryConfigFile: "Ocelot/ocelot.json",
		reloadOnChange: true
	)
	.AddEnvironmentVariables();

// Register services
services.AddControllers();
services.AddEndpointsApiExplorer();
services.AddSwaggerGen();
services.AddOcelot(builder.Configuration);

// 🐨 Add Koalesce for Ocelot
services.AddKoalesce(builder.Configuration)
	.ForOpenAPI(options =>
	{
		// Change the SecurityMode if you want to test Auth scheme options 
		// "None" to disable security (not recommended for production)
		// "JWT" for JWT Bearer in header
		// "ApiKey" for ApiKey in header
		// "Basic" for Basic Authentication
		// "OAuth2ClientCredentials" for OAuth2 Client Credentials flow
		// "OAuth2AuthCode" for OAuth2 Authorization Code flow
		// "OpenIdConnect" for OpenID Connect
		string securityScenario = "None";
		switch (securityScenario.ToUpper())
		{
			case "JWT":
				LocalAuthConfigurationHelpers.ConfigureJwtScenario(options);
				break;
			case "APIKEY":
				LocalAuthConfigurationHelpers.ConfigureApiKeyScenario(options);
				break;
			case "BASIC":
				LocalAuthConfigurationHelpers.ConfigureBasicAuthScenario(options);
				break;
			case "OAUTH2CLIENTCREDENTIALS":
				LocalAuthConfigurationHelpers.ConfigureOAuth2ClientCredentialsGatewaySecurity(options);
				break;
			case "OAUTH2AUTHCODE":
				LocalAuthConfigurationHelpers.ConfigureOAuth2AuthCodeScenario(options);
				break;
			case "OPENIDCONNECT":
				LocalAuthConfigurationHelpers.ConfigureOpenIdConnectScenario(options);
				break;
			case "NONE":
			default:
				// No security extensions applied
				break;
		}
	});

// Build app
var app = builder.Build();

// 🐨 Accessing the automatically registered OcelotOptions based on your appsettings.json
KoalesceOptions koalesceOptions;
using (var scope = app.Services.CreateScope())
{
	koalesceOptions = scope.ServiceProvider
		.GetRequiredService<IOptions<KoalesceOptions>>().Value;
}

// App pipeline
app.UseWebSockets();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// 🐨 Enable Koalesce before Swagger Middleware
app.UseKoalesce();

// Enable Swagger 
app.UseSwagger();
app.UseSwaggerUI(c =>
{
	c.SwaggerEndpoint(koalesceOptions.MergedDocumentPath, koalesceOptions.Title);
});

await app.UseOcelot();
await app.RunAsync();