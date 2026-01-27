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

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
	c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
	{
		Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
		Scheme = "bearer",
		BearerFormat = "JWT",
		In = Microsoft.OpenApi.Models.ParameterLocation.Header,
		Name = "Authorization",
		Description = "JWT Authorization header using the Bearer scheme."
	});

	c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
	{
		{
			new Microsoft.OpenApi.Models.OpenApiSecurityScheme
			{
				Reference = new Microsoft.OpenApi.Models.OpenApiReference
				{
					Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
					Id = "Bearer"
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

// Customers list
var customers = new List<Customer>
{
	new Customer(Guid.NewGuid(), "John Doe"),
	new Customer(Guid.NewGuid(), "Jane Smith")
};

// List all
app.MapGet("/api/customers", () => customers)
	.WithName("ListCustomers");

// Get by Id
app.MapGet("/api/customers/{id:guid}", (Guid id) =>
{
	var customer = customers.SingleOrDefault(c => c.Id == id);
	return customer is not null ? Results.Ok(customer) : Results.NotFound();
})
.WithName("GetCustomerById");

// Create a customer
app.MapPost("/api/customers", (Customer newCustomer) =>
{
	customers.Add(newCustomer);
	return Results.Created($"/api/customers/{newCustomer.Id}", newCustomer);
})
.WithName("CreateCustomer");

await app.RunAsync();

public record Customer(Guid Id, string Name);
