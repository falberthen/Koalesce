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
services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseCors(corsPolicy);
app.UseHttpsRedirection();
app.UseRouting();


var random = new Random();
var maxQuantity = 1000;
var inventoryProducts = new List<InventoryProduct>
{
	new InventoryProduct(Guid.NewGuid(), "Rocket", random.Next(maxQuantity)),
	new InventoryProduct(Guid.NewGuid(), "Telescope", random.Next(maxQuantity)),
	new InventoryProduct(Guid.NewGuid(), "Processor", random.Next(maxQuantity)),
};

// Get all products
app.MapGet("/api/products", () =>
{
	return inventoryProducts;
})
.WithName("GetProducts");

await app.RunAsync();

public record InventoryProduct(Guid Id, string Name, int QuantityInStock);

