namespace Koalesce.Tests.Integration.RestAPIs;

public class CustomersApi
{
	public static WebApplication Create()
	{
		var builder = WebApplication.CreateBuilder();

		// Bind to a fixed port for testing
		builder.WebHost.UseKestrel()
			.UseUrls("http://localhost:8001");

		var app = builder.Build();

		app.MapGet("/swagger/v1/swagger.json", async () =>
		{
			var json = """
            {
                "openapi": "3.0.1",
                "info": { "title": "Customers API", "version": "v1" },
                "paths": {
                    "/api/customers": {
                        "get": {
                            "summary": "Get Customers",
                            "security": [
                                { "api_key": [] } 
                            ]
                        }
                    }
                },
                "components": {
                    "securitySchemes": {
                        "api_key": {
                            "type": "apiKey",
                            "name": "X-API-Key",
                            "in": "header"
                        }
                    }
                }
            }
            """;
			return Results.Text(json, "application/json");
		});

		return app;
	}
}
