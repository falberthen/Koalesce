using Koalesce.Extensions;
using Koalesce.Options;
using Microsoft.Extensions.Options;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
IServiceCollection services = builder.Services;

// First, register essential services like controllers and authentication
services.AddControllers();
services.AddEndpointsApiExplorer();

// Register Swagger services before Koalesce
services.AddSwaggerGen();

// 🐨 Register Koalesce
services.AddKoalesce(builder.Configuration);

// Build the app
var app = builder.Build();

KoalesceOptions openApiOptions;
using (var scope = app.Services.CreateScope())
{
	openApiOptions = scope.ServiceProvider
		.GetRequiredService<IOptions<KoalesceOptions>>().Value;
}

// Add pipeline
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
	c.SwaggerEndpoint(openApiOptions.MergedEndpoint, openApiOptions.Title);
});

app.Run();
