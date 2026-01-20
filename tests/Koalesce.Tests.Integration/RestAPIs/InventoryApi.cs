namespace Koalesce.Tests.Integration.RestAPIs;

public class InventoryApi
{
	public static WebApplication Create()
	{
		var builder = WebApplication.CreateBuilder();

		// Bind to a fixed port for testing
		builder.WebHost.UseKestrel()
			.UseUrls("http://localhost:8003");

		var app = builder.Build();

		app.MapGet("/swagger/v1/swagger.json", async () =>
		{
			var json = """
            {
                "openapi": "3.0.1",
                "info": { "title": "Inventory API", "version": "v1" },
                "paths": {
                    "/api/products": {
                        "get": {
                            "summary": "Get Inventory Products",
                            "responses": {
                                "200": {
                                    "description": "OK",
                                    "content": {
                                        "application/json": {
                                            "schema": {
                                                "type": "array",
                                                "items": { "$ref": "#/components/schemas/Product" }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
                "components": {
                    "schemas": {
                        "Product": {
                            "type": "object",
                            "properties": {
                                "id": { "type": "string", "format": "uuid" },
                                "name": { "type": "string" },
                                "quantityInStock": { "type": "integer" }
                            }
                        }
                    }
                },
                "tags": [
                    {
                        "name": "Inventory Products",
                        "description": "Operations related to inventory products"
                    }
                ]
            }
            """;
			return Results.Text(json, "application/json");
		});

		return app;
	}
}
