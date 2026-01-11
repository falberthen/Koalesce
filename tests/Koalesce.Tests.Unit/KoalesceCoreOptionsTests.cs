namespace Koalesce.Tests.Unit;

[Collection("Koalesce Core Options Unit Tests")]
public class KoalesceCoreOptionsTests : KoalesceUnitTestBase
{
	[Fact]
	public void AddKoalesce_WhenNonRequiredConfigValuesAreMissing_ShouldUseDefaultValues()
	{
		// Arrange
		var configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				{ "Koalesce:Sources:0:Url", "https://localhost:5001/swagger.json" },
				{ "Koalesce:MergedDocumentPath", "/swagger/v1/merged.json" }
			})
			.Build();

		Services.AddKoalesce(configuration)
			.AddProvider<DummyProvider, KoalesceOptions>();

		var provider = Services.BuildServiceProvider();

		// Act
		var options = provider.GetService<IOptions<KoalesceOptions>>()?.Value;

		// Assert
		Assert.NotNull(options);
		Assert.Equal(KoalesceOptions.TitleDefaultValue, options.Title); // Default title should be set		
	}

	[Fact]
	public void AddKoalesce_WhenValidConfiguration_ShouldBindKoalesceOptions()
	{
		// Arrange
		var configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				{ "Koalesce:Title", "My Koalesced API" },
				{ "Koalesce:MergedDocumentPath", "/swagger/v1/apigateway.yaml" },
				{ "Koalesce:Sources:0:Url", "https://localhost:5001/swagger/v1/swagger.json" },
				{ "Koalesce:Sources:1:Url", "https://localhost:5002/swagger/v1/swagger.json" }
			})
			.Build();

		Services.AddKoalesce(configuration)
			.AddProvider<DummyProvider, DummyOptions>();

		var provider = Services.BuildServiceProvider();

		var expectedRoutes = new List<SourceDefinition>()
		{
			new SourceDefinition { Url = "https://localhost:5001/swagger/v1/swagger.json" },
			new SourceDefinition { Url = "https://localhost:5002/swagger/v1/swagger.json" }
		};

		// Act
		var options = provider.GetService<IOptions<DummyOptions>>()?.Value;

		// Assert
		Assert.NotNull(options);
		Assert.Equal("My Koalesced API", options.Title);
		Assert.Equal("/swagger/v1/apigateway.yaml", options.MergedDocumentPath);
		Assert.Equal(expectedRoutes, options.Sources);
	}

	[Fact]
	public void AddKoalesce_WhenKoalesceSectionIsMissing_ShouldThrowKoalesceConfigurationNotFoundException()
	{
		// Arrange: Empty configuration (no "Koalesce" section)
		var emptyConfiguration = new ConfigurationBuilder().Build();

		// Act & Assert: Expect custom exception when attempting to use Koalesce
		Assert.Throws<KoalesceConfigurationNotFoundException>(() =>
			Services.AddKoalesce(emptyConfiguration)
				.AddProvider<DummyProvider, KoalesceOptions>());
	}

	[Fact]
	public void Koalesce_WhenOpenApiSourcesContainInvalidSourceUrl_ShouldThrowValidationException()
	{
		// Arrange
		var configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				{ "Koalesce:MergedDocumentPath", "/swagger/v1/swagger.json" },
				{ "Koalesce:Sources:0:Url", "https://api1.com/swagger/v1/swagger.json" },
				{ "Koalesce:Sources:1:Url", "localhost:8002/swagger/v1/swagger.json" }
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
		
		Assert.Contains("must be a valid absolute URL", exception.Message);
		Assert.Contains("index 1", exception.Message);
	}
}