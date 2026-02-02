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
services.AddSwaggerGen(options =>
{
	var serverUrl = builder.Configuration["ServerUrl"];
	if (!string.IsNullOrEmpty(serverUrl))
		options.AddServer(new OpenApiServer { Url = serverUrl });
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


var random = new Random();
var maxQuantity = 1000;
var inventoryProducts = new List<Product>
{
	new Product(Guid.NewGuid(), "Rocket", random.Next(maxQuantity)),
	new Product(Guid.NewGuid(), "Telescope", random.Next(maxQuantity)),
	new Product(Guid.NewGuid(), "Processor", random.Next(maxQuantity)),
};

// Get all products
app.MapGet("/api/products", () =>
{
	return inventoryProducts;
})
.WithName("GetProducts");

await app.RunAsync();

public record Product(Guid Id, string Name, int QuantityInStock);

