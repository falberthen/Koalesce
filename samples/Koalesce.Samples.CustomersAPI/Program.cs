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

// Customers list
var customers = new List<Customer>
{
	new Customer(1, "John Doe"),
	new Customer(2, "Jane Smith")
};

// List all
app.MapGet("/api/customers", () => customers)
	.WithName("ListCustomers");

// Get by Id
app.MapGet("/api/customers/{id:int}", (int id) =>
{
	var customer = customers.FirstOrDefault(c => c.Id == id);
	return customer is not null ? Results.Ok(customer) : Results.NotFound();
})
.WithName("GetCustomerById");

// Create a customer
app.MapPost("/api/customers", (Customer newCustomer) =>
{
	if (newCustomer.Id == 0)
	{
		newCustomer = newCustomer with { Id = customers.Count + 1 }; // Auto-assign an ID
	}

	customers.Add(newCustomer);
	return Results.Created($"/api/customers/{newCustomer.Id}", newCustomer);
})
.WithName("CreateCustomer");

app.Run();

public record Customer(int Id, string Name);
