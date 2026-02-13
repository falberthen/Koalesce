namespace Koalesce.Tests;

[Collection("Koalesce Options Unit Tests")]
public class OptionsUnitTests : KoalesceUnitTestBase
{
	[Fact]
	public void Koalesce_WhenValidConfiguration_ShouldBindKoalesceOptions()
	{
		// Arrange
		var appSettingsStub = new
		{
			Koalesce = new
			{
				Info = new { Title = "My Koalesced API" },
				MergedEndpoint = "/v1/mergedapidefinition.yaml",
				Sources = new List<ApiSource>
				{
					new ApiSource { Url = "https://localhost:5001/v1/apidefinition.json" },
					new ApiSource { Url = "https://localhost:5002/v1/apidefinition.json" }
				}
			}
		};

		var configuration = ConfigurationHelper
			.BuildConfigurationFromObject(appSettingsStub);

		Services.AddKoalesce(configuration);

		var provider = Services.BuildServiceProvider();

		var expectedRoutes = new List<ApiSource>()
		{
			new ApiSource { Url = "https://localhost:5001/v1/apidefinition.json" },
			new ApiSource { Url = "https://localhost:5002/v1/apidefinition.json" }
		};

		// Act
		var options = provider.GetService<IOptions<KoalesceOptions>>()?.Value;

		// Assert
		Assert.NotNull(options);
		Assert.Equal("My Koalesced API", options.Info.Title);
		Assert.Equal("/v1/mergedapidefinition.yaml", options.MergedEndpoint);

		Assert.Equal(expectedRoutes.Count, options.Sources.Count);
		Assert.Equal(expectedRoutes[0].Url, options.Sources[0].Url);
		Assert.Equal(expectedRoutes[1].Url, options.Sources[1].Url);
	}

	[Fact]
	public void Koalesce_WhenNonRequiredConfigValuesAreMissing_ShouldUseDefaultValues()
	{
		// Arrange
		var appSettingsStub = new
		{
			Koalesce = new
			{
				MergedEndpoint = "/v1/mergedapidefinition.json",
				Sources = new List<ApiSource>
			{
				new ApiSource { Url = "https://localhost:5001/v1/apidefinition.json" }
			}
			}
		};

		var configuration = ConfigurationHelper
			.BuildConfigurationFromObject(appSettingsStub);

		Services.AddKoalesce(configuration);

		var provider = Services.BuildServiceProvider();

		// Act
		var options = provider.GetService<IOptions<KoalesceOptions>>()?.Value;

		// Assert
		Assert.NotNull(options);
		Assert.Equal(KoalesceConstants.DefaultTitle, options.Info.Title); // Default title should be set
	}

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

		Assert.Contains(KoalesceConstants.ApiGatewayBaseUrlValidationError, exception.Message);
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