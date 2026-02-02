using Koalesce.Extensions;
using Koalesce.Options;
using Microsoft.Extensions.Options;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
IServiceCollection services = builder.Services;

// Load Ocelot configuration from environment-specific folder
var ocelotFolder = builder.Environment.IsEnvironment("Docker") ? "Ocelot.Docker" : "Ocelot";
builder.Configuration
	.SetBasePath(Directory.GetCurrentDirectory())
	.AddOcelot(
		folder: ocelotFolder,
		env: builder.Environment,
		mergeTo: MergeOcelotJson.ToMemory,
		reloadOnChange: true
	)
	.AddEnvironmentVariables();

// Register services
services.AddControllers();
services.AddEndpointsApiExplorer();
services.AddSwaggerGen();
services.AddOcelot(builder.Configuration);

// 🐨 Register Koalesce
services.AddKoalesce(builder.Configuration);

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
	c.SwaggerEndpoint(koalesceOptions.MergedEndpoint, koalesceOptions.Title);
});

await app.UseOcelot();
await app.RunAsync();