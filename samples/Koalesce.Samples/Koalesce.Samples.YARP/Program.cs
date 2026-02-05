using Koalesce.Extensions;
using Koalesce.Options;
using Microsoft.Extensions.Options;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
IServiceCollection services = builder.Services;

// Load YARP configuration from environment-specific file
var yarpFile = builder.Environment.IsEnvironment("Docker") ? "yarp.Docker.json" : "yarp.json";
builder.Configuration
	.SetBasePath(Directory.GetCurrentDirectory())
	.AddJsonFile(yarpFile, optional: false, reloadOnChange: true);

// Register services
services.AddControllers();
services.AddEndpointsApiExplorer();
services.AddSwaggerGen();

// Add YARP reverse proxy
services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// Register Koalesce
services.AddKoalesce(builder.Configuration);

// Build app
var app = builder.Build();

// Access the Koalesce options
KoalesceOptions koalesceOptions;
using (var scope = app.Services.CreateScope())
{
    koalesceOptions = scope.ServiceProvider
        .GetRequiredService<IOptions<KoalesceOptions>>().Value;
}

// App pipeline
app.UseRouting();
app.UseAuthorization();
app.MapControllers();

// Enable Koalesce
app.UseKoalesce();

// Enable Swagger
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint(koalesceOptions.MergedEndpoint, koalesceOptions.Title);
});

// Map YARP reverse proxy
app.MapReverseProxy();

await app.RunAsync();
