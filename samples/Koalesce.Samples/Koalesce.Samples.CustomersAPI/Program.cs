using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);
IServiceCollection services = builder.Services;

// Define CORS Policy
const string corsPolicy = "CorsPolicy";
services.AddCors(o =>
	o.AddPolicy(corsPolicy, cors =>
	{
		cors
			.AllowAnyMethod()
			.AllowAnyHeader()
			.AllowCredentials()
			.SetIsOriginAllowed(_ => true);
	})
);

services.AddEndpointsApiExplorer();

services.AddSwaggerGen(options =>
{
	var serverUrl = builder.Configuration["ServerUrl"];
	if (!string.IsNullOrEmpty(serverUrl))
		options.AddServer(new OpenApiServer { Url = serverUrl });

	options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
	{
		Type = SecuritySchemeType.Http,
		Scheme = "bearer",
		BearerFormat = "JWT",
		In = ParameterLocation.Header,
		Name = "Authorization",
		Description = "JWT Authorization header using the Bearer scheme."
	});

	options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
	{
		[new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
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

var customersGroup = app.MapGroup("/api/customers")
	.WithTags("Customers");

// Customers list
var customers = new List<Customer>
{
	new Customer(Guid.NewGuid(), "John Doe"),
	new Customer(Guid.NewGuid(), "Jane Smith")
};

// List all
customersGroup.MapGet("/", () => customers)
	.WithName("ListCustomers");

// Get by Id
customersGroup.MapGet("/{id:guid}", (Guid id) =>
{
	var customer = customers.SingleOrDefault(c => c.Id == id);
	return customer is not null ? Results.Ok(customer) : Results.NotFound();
})
.WithName("GetCustomerById");

// Create a customer
customersGroup.MapPost("/", (Customer newCustomer) =>
{
	customers.Add(newCustomer);
	return Results.Created($"/{newCustomer.Id}", newCustomer);
})
.WithName("CreateCustomer");

await app.RunAsync();

public record Customer(Guid Id, string Name);
