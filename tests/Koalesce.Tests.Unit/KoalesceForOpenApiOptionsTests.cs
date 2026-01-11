using Koalesce.OpenAPI;
using Koalesce.OpenAPI.Constants;
using Koalesce.OpenAPI.Extensions;

namespace Koalesce.Tests.Unit;

[Collection("Koalesce ForOpenAPI Unit Tests")]
public class KoalesceForOpenApiOptionsTests : KoalesceUnitTestBase
{
	[Fact]
	public void Koalesce_WhenForOpenAPI_WhenUsingGateway_WithNoAuthSchemeDefined_ShouldThrowValidationException()
	{
		// Arrange
		var configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				{ "Koalesce:MergedDocumentPath", "/swagger/v1/swagger.json" },
				{ "Koalesce:Sources:0:Url", "https://api1.com/swagger/v1/swagger.json" },
				{ "Koalesce:ApiGatewayBaseUrl", "http://localhost:5000" }
			})
			.Build();

		Services.AddKoalesce(configuration)
			.ForOpenAPI();

		var provider = Services.BuildServiceProvider();

		// Act & Assert
		var exception = Assert.Throws<OptionsValidationException>(() =>
		{
			var options = provider.GetRequiredService<IOptions<OpenApiOptions>>().Value;
		});

		Assert.Contains(OpenAPIConstants.RequiredGatewaySecuritySchemeValidationError, exception.Message);
	}

	[Fact]
	public void Koalesce_WhenForOpenAPI_WhenUsingGateway_WithInvalidGatewayUrl_ShouldThrowValidationException()
	{
		// Arrange
		var configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				{ "Koalesce:MergedDocumentPath", "/swagger/v1/swagger.json" },
				{ "Koalesce:Sources:0:Url", "https://api1.com/swagger/v1/swagger.json" },
				{ "Koalesce:GatewaySecurityScheme:Type", "Http" },
				{ "Koalesce:GatewaySecurityScheme:Scheme", "bearer" },				
				{ "Koalesce:ApiGatewayBaseUrl", "localhost:5000" } // Invalid URL (missing scheme)
			})
			.Build();

		Services.AddKoalesce(configuration)
			.ForOpenAPI();

		var provider = Services.BuildServiceProvider();

		// Act & Assert
		var exception = Assert.Throws<OptionsValidationException>(() =>
		{
			var options = provider.GetRequiredService<IOptions<OpenApiOptions>>().Value;
		});

		Assert.Contains(OpenAPIConstants.ApiGatewayBaseUrlValidationError, exception.Message);
	}
}