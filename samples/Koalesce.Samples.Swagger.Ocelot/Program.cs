using Koalesce.Core.Extensions;
using Koalesce.OpenAPI.Extensions;
using Koalesce.Samples.Swagger.Ocelot;
using Microsoft.Extensions.Options;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
IServiceCollection services = builder.Services;

// Detect which Security Scenario to run based on Environment Variable
string securityScenario = builder.Configuration["KoalesceSample:SecurityMode"] ?? "None";
Console.WriteLine($"🐨 Starting Koalesce Sample in mode: {securityScenario}");

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