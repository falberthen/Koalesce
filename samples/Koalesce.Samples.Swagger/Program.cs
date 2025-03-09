using Koalesce.Core;
using Koalesce.OpenAPI;
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
OpenApiOptions openApiOptions;
using (var scope = app.Services.CreateScope())
{
	openApiOptions = scope.ServiceProvider
		.GetRequiredService<IOptions<OpenApiOptions>>().Value;
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
	c.SwaggerEndpoint(openApiOptions.MergedOpenApiPath, openApiOptions.Title);
});

app.Run();
