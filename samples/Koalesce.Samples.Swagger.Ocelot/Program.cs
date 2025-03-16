using Koalesce.Core.Extensions;
using Koalesce.OpenAPI;
using Koalesce.Samples.Kiota;
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
	.ForOpenAPI();

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
	c.SwaggerEndpoint(koalesceOptions.MergedOpenApiPath, koalesceOptions.Title);
});

// Ensure Ocelot is fully initialized before generating the client
await app.UseOcelot();

app.RunAsync(); // Start the API in the background
				// Wait a few seconds before generating the Kiota client
await Task.Delay(500);

// Wrapper to call Koalesced API and return results
var wrapper = new ApiWrapper(koalesceOptions);

/// Fetch customers and products
bool generateNewClient = false;  // Set it to true to rebuild the API client.
await wrapper.ShowKoalescedResultAsync(generateNewClient);

await app.WaitForShutdownAsync();
