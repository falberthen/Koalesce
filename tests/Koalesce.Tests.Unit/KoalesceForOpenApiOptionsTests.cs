namespace Koalesce.Tests.Unit;

[Collection("Koalesce ForOpenAPI Unit Tests")]
public class KoalesceForOpenApiOptionsTests : KoalesceUnitTestBase
{
	[Fact]
	public void Koalesce_WhenForOpenAPI_WhenUsingGateway_WithNoAuthSchemeDefined_ShouldThrowValidationException()
	{
		// Arrange		
		var appSettingsStub = new
		{
			Koalesce = new
			{
				MergedDocumentPath = "/v1/mergedapidefinition.json",
				Sources = new[]
				{
					new { Url = "https://api1.com/v1/apidefinition.json" }
				},
				ApiGatewayBaseUrl = "http://localhost:5000"
				// GatewaySecurityScheme is omitted to cause the validation error
			}
		};

		var configuration = ConfigurationHelper
			.BuildConfigurationFromObject(appSettingsStub);

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
		var appSettingsStub = new
		{
			Koalesce = new
			{
				MergedDocumentPath = "/v1/mergedapidefinition.json",
				Sources = new[]
				{
					new { Url = "https://api1.com/v1/apidefinition.json" }
				},
				GatewaySecurityScheme = new
				{
					Type = SecuritySchemeType.Http.ToString(),
					Scheme = "bearer"
				},
				ApiGatewayBaseUrl = "localhost:5000" // Invalid URL (no scheme http/https)
			}
		};

		var configuration = ConfigurationHelper
			.BuildConfigurationFromObject(appSettingsStub);

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