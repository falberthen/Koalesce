using Microsoft.OpenApi;

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
	c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
	{
		Type = SecuritySchemeType.ApiKey,
		In = ParameterLocation.Header,
		Name = "X-API-KEY",
		Description = "API Key authentication using X-API-KEY header"
	});

	c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
	{
		[new OpenApiSecuritySchemeReference("ApiKey", document)] = new List<string>()
	});
});

var app = builder.Build();

if (!app.Environment.IsProduction())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseCors(corsPolicy);

if (!app.Environment.IsEnvironment("Docker"))
	app.UseHttpsRedirection();

app.UseRouting();


// #################### MINIMAL API DEFINITION  ####################

// Public
var productsGroup = app.MapGroup("/api/products")
	.WithTags("Products");
// Admin (Remove from ExcludePaths to be included in the OpenAPI spec)
var adminProductsGroup = app.MapGroup("/api/admin/products")
	.WithTags("Products");

var products = new List<Product>
{
	new Product(Guid.NewGuid(), "Laptop"),
	new Product(Guid.NewGuid(), "Joystick"),
	new Product(Guid.NewGuid(), "Guitar")
};

// Get all products
productsGroup.MapGet("/", () =>
{
	return products;
})
.WithName("GetProducts");

// Create a product (admin only, to be a skipped endpoint using ExcludePaths)
adminProductsGroup.MapPost("/", (Product newProduct) =>
{
	products.Add(newProduct);
	return Results.Created($"/api/admin/products/{newProduct.Id}", newProduct);
})
.WithName("CreateProduct");

await app.RunAsync();

public record Product(Guid Id, string Name);
