namespace Koalesce.Tests.Unit;

public class KoalesceOptionsTests : KoalesceUnitTestBase
{
	[Fact]
	public void AddKoalesce_WhenNonRequiredConfigValuesAreMissing_ShouldUseDefaultValues()
	{
		// Arrange
		var configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				{ "Koalesce:OpenApiSources:0:Url", "https://localhost:5001/swagger.json" },
				{ "Koalesce:MergedOpenApiPath", "/swagger/v1/merged.json" }
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
				{ "Koalesce:MergedOpenApiPath", "/swagger/v1/apigateway.yaml" },
				{ "Koalesce:OpenApiSources:0:Url", "https://localhost:5001/swagger/v1/swagger.json" },
				{ "Koalesce:OpenApiSources:1:Url", "https://localhost:5002/swagger/v1/swagger.json" }
			})
			.Build();

		Services.AddKoalesce(configuration)
			.AddProvider<DummyProvider, KoalesceOptions>();

		var provider = Services.BuildServiceProvider();

		var expectedRoutes = new List<OpenApiSourceDefinition>()
		{
			new OpenApiSourceDefinition { Url = "https://localhost:5001/swagger/v1/swagger.json" },
			new OpenApiSourceDefinition { Url = "https://localhost:5002/swagger/v1/swagger.json" }
		};

		// Act
		var options = provider.GetService<IOptions<KoalesceOptions>>()?.Value;

		// Assert
		Assert.NotNull(options);
		Assert.Equal("My Koalesced API", options.Title);
		Assert.Equal("/swagger/v1/apigateway.yaml", options.MergedOpenApiPath);
		Assert.Equal(expectedRoutes, options.OpenApiSources);
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
}