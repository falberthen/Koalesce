var builder = WebApplication.CreateBuilder(args);
IServiceCollection services = builder.Services;

// Define CORS Policy
const string corsPolicy = "CorsPolicy";
services.AddCors(o =>
	o.AddPolicy(corsPolicy, builder =>
	{
		builder
		.AllowAnyMethod()
		.AllowAnyHeader()
		.AllowCredentials()
		.SetIsOriginAllowed(x => true);
	})
);

services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
	c.AddSecurityDefinition("ApiKey", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
	{
		Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
		In = Microsoft.OpenApi.Models.ParameterLocation.Header,
		Name = "X-API-KEY",
		Description = "API Key authentication using X-API-KEY header"
	});

	c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
	{
		{
			new Microsoft.OpenApi.Models.OpenApiSecurityScheme
			{
				Reference = new Microsoft.OpenApi.Models.OpenApiReference
				{
					Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
					Id = "ApiKey"
				}
			},
			Array.Empty<string>()
		}
	});
});


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseCors(corsPolicy);
app.UseHttpsRedirection();
app.UseRouting();

// Get all products
app.MapGet("/api/products", () =>
{
	return new List<Product>
	{
		new Product(Guid.NewGuid(), "Laptop"),
		new Product(Guid.NewGuid(), "Joystick"),
		new Product(Guid.NewGuid(), "Guitar")
	};
})
.WithName("GetProducts");

await app.RunAsync();

public record Product(Guid Id, string Name);

