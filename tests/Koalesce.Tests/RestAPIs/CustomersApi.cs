namespace Koalesce.Tests.RestAPIs;

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
                            "responses": {
                                "200": {
                                    "description": "OK",
                                    "content": {
                                        "application/json": {
                                            "schema": {
                                                "type": "array",
                                                "items": { "$ref": "#/components/schemas/Customer" }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    },
                    "/api/customers/{id}": {
                        "get": {
                            "summary": "Get Customer by Id",
                            "parameters": [
                                { "name": "id", "in": "path", "required": true, "schema": { "type": "string" } }
                            ],
                            "responses": {
                                "200": {
                                    "description": "OK",
                                    "content": {
                                        "application/json": {
                                            "schema": { "$ref": "#/components/schemas/CustomerDetails" }
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
                "components": {
                    "schemas": {
                        "Customer": {
                            "type": "object",
                            "properties": {
                                "id": { "type": "string", "format": "uuid" },
                                "name": { "type": "string" }
                            }
                        },
                        "CustomerDetails": {
                            "type": "object",
                            "properties": {
                                "id": { "type": "string", "format": "uuid" },
                                "name": { "type": "string" },
                                "email": { "type": "string" },
                                "address": { "type": "string" }
                            }
                        }
                    },
                    "securitySchemes": {
                        "bearerAuth": {
                            "type": "http",
                            "scheme": "bearer",
                            "bearerFormat": "JWT"
                        }
                    }
                },
                "security": [
                    { "bearerAuth": [] }
                ],
                "tags": [
                   {
                      "name": "Customers",
                      "description": "Operations related to customers"
                   }
                ]
            }
            """;
			return Results.Text(json, "application/json");
		});

		return app;
	}
}
