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

// Get all products
app.MapGet("/api/products", () =>
{
	return new List<Product>
	{
		new Product("Rocket"),
		new Product("Telescope"),
		new Product("Processor")
	};
})
.WithName("GetProducts");

app.Run();

public record Product(string Name);

