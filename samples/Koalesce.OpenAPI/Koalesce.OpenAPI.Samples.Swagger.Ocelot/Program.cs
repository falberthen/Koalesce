using Koalesce.Core.Extensions;
using Koalesce.OpenAPI.Extensions;
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

// 🐨 Register Koalesce for OpenAPI
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

// 🐨 Enable Koalesce
app.UseKoalesce();

// Enable Swagger 
app.UseSwagger();
app.UseSwaggerUI(c =>
{
	c.SwaggerEndpoint(koalesceOptions.MergedDocumentPath, koalesceOptions.Title);
});

await app.UseOcelot();
await app.RunAsync();