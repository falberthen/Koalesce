using Koalesce.Core.Extensions;
using Koalesce.OpenAPI.Extensions;
using Koalesce.OpenAPI.Options;
using Microsoft.Extensions.Options;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
IServiceCollection services = builder.Services;

// First, register essential services like controllers and authentication
services.AddControllers();
services.AddEndpointsApiExplorer();

// Register Swagger services before Koalesce
services.AddSwaggerGen();

// 🐨 Register Koalesce
services.AddKoalesce(builder.Configuration)
	.ForOpenAPI();

// Build the app
var app = builder.Build();

// 🐨 Accessing the automatically registered OpenApiOptions based on your appsettings.json
KoalesceOpenApiOptions openApiOptions;
using (var scope = app.Services.CreateScope())
{
	openApiOptions = scope.ServiceProvider
		.GetRequiredService<IOptions<KoalesceOpenApiOptions>>().Value;
}

// Add pipeline
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
	c.SwaggerEndpoint(openApiOptions.MergedDocumentPath, openApiOptions.Title);
});

app.Run();
