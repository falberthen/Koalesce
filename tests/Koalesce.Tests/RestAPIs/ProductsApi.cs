namespace Koalesce.Tests.RestAPIs;

public class ProductsApi
{
	public static WebApplication Create()
	{
		var builder = WebApplication.CreateBuilder();

		// Bind to a fixed port for testing
		builder.WebHost.UseKestrel()
			.UseUrls("http://localhost:8002");

		var app = builder.Build();

		app.MapGet("/swagger/v1/swagger.json", async () =>
		{
			var json = """
            {
                "openapi": "3.0.1",
                "info": { "title": "Products API", "version": "v1" },
                "paths": {
                    "/api/products": {
                        "get": {
                            "summary": "Get Products",
                            "tags": ["Products"],
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
                                "name": { "type": "string" }
                            }
                        }
                    },
                    "securitySchemes": {
                        "api_key": {
                            "type": "apiKey",
                            "name": "X-API-Key",
                            "in": "header"
                        }
                    }
                },
                "security": [
                  { "api_key": [] }
                ],
                "tags": [
                {
                    "name": "Products",
                    "description": "Operations related to products"
                }
               ]
            }
            """;
			return Results.Text(json, "application/json");
		});

		return app;
	}
}
