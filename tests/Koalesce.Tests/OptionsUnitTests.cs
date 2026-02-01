namespace Koalesce.Tests;

[Collection("Koalesce Options Unit Tests")]
public class OptionsUnitTests : KoalesceUnitTestBase
{
	[Fact]
	public void Koalesce_WithInvalidGatewayUrl_ShouldThrowValidationException()
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
				ApiGatewayBaseUrl = "localhost:5000" // Invalid URL (no scheme http/https)
			}
		};

		var configuration = ConfigurationHelper
			.BuildConfigurationFromObject(appSettingsStub);

		Services.AddKoalesce(configuration);

		var provider = Services.BuildServiceProvider();

		// Act & Assert
		var exception = Assert.Throws<OptionsValidationException>(() =>
		{
			var options = provider.GetRequiredService<IOptions<Options.KoalesceOptions>>().Value;
		});

		Assert.Contains(Constants.KoalesceConstants.ApiGatewayBaseUrlValidationError, exception.Message);
	}

	[Fact]
	public void Koalesce_WithEmptyGatewayUrl_ShouldNotThrowValidationException()
	{
		// Arrange - Empty string is treated as Aggregation Mode
		var appSettingsStub = new
		{
			Koalesce = new
			{
				MergedDocumentPath = "/v1/mergedapidefinition.json",
				Sources = new[]
				{
					new { Url = "https://api1.com/v1/apidefinition.json" }
				},
				ApiGatewayBaseUrl = "" // Empty = Aggregation Mode
			}
		};

		var configuration = ConfigurationHelper
			.BuildConfigurationFromObject(appSettingsStub);

		Services.AddKoalesce(configuration);

		var provider = Services.BuildServiceProvider();

		// Act & Assert - Should NOT throw
		var options = provider.GetRequiredService<IOptions<Options.KoalesceOptions>>().Value;

		Assert.NotNull(options);
		Assert.Empty(options.ApiGatewayBaseUrl);
	}
}