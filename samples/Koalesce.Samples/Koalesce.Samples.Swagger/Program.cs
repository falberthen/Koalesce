using Koalesce.Extensions;
using Koalesce.Options;
using Microsoft.Extensions.Options;
using Polly;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
IServiceCollection services = builder.Services;

// First, register essential services like controllers and authentication
services.AddControllers();
services.AddEndpointsApiExplorer();

// Register Swagger services before Koalesce
services.AddSwaggerGen();

// 🐨 Register Koalesce with HttpClient customization
services.AddKoalesce(builder.Configuration, configureHttpClient: httpClientBuilder =>
{
	// Example 1: Skip SSL validation for self-signed certificates (dev environments)
	httpClientBuilder.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
	{
		ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
	});

	// Example 2: Add retry policy with Polly (requires Microsoft.Extensions.Http.Polly)
	// You can make some apis not available to test this behavior
	httpClientBuilder.AddTransientHttpErrorPolicy(policy =>
		policy.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));

	// Example 3: Add custom headers for authentication
	httpClientBuilder.ConfigureHttpClient(client =>
		client.DefaultRequestHeaders.Add("X-Api-Key", "your-api-key"));
});

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
