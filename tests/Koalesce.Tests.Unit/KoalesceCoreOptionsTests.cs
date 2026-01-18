using Koalesce.OpenAPI.Options;

namespace Koalesce.Tests.Unit;

[Collection("Koalesce Core Options Unit Tests")]
public class KoalesceCoreOptionsTests : KoalesceUnitTestBase
{
	[Fact]
	public void AddKoalesce_WhenNonRequiredConfigValuesAreMissing_ShouldUseDefaultValues()
	{
		// Arrange
		var appSettingsStub = new
		{
			Koalesce = new KoalesceOptions
			{
				// Title omitido propositalmente para testar o default
				MergedDocumentPath = "/v1/mergedapidefinition.json",
				Sources = new List<SourceDefinition>
				{
					new SourceDefinition { Url = "https://localhost:5001/v1/apidefinition.json" }
				}
			}
		};

		var configuration = ConfigurationHelper
			.BuildConfigurationFromObject(appSettingsStub);

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
		var appSettingsStub = new
		{
			Koalesce = new KoalesceOptions
			{
				Title = "My Koalesced API",
				MergedDocumentPath = "/v1/mergedapidefinition.yaml",
				Sources = new List<SourceDefinition>
				{
					new SourceDefinition { Url = "https://localhost:5001/v1/apidefinition.json" },
					new SourceDefinition { Url = "https://localhost:5002/v1/apidefinition.json" }
				}
			}
		};

		var configuration = ConfigurationHelper
			.BuildConfigurationFromObject(appSettingsStub);

		Services.AddKoalesce(configuration)
			.AddProvider<DummyProvider, DummyOptions>();

		var provider = Services.BuildServiceProvider();

		var expectedRoutes = new List<SourceDefinition>()
		{
			new SourceDefinition { Url = "https://localhost:5001/v1/apidefinition.json" },
			new SourceDefinition { Url = "https://localhost:5002/v1/apidefinition.json" }
		};

		// Act
		var options = provider.GetService<IOptions<DummyOptions>>()?.Value;

		// Assert
		Assert.NotNull(options);
		Assert.Equal("My Koalesced API", options.Title);
		Assert.Equal("/v1/mergedapidefinition.yaml", options.MergedDocumentPath);

		// Nota: Isso assume que SourceDefinition é um record ou implementa Equals corretamente
		Assert.Equal(expectedRoutes.Count, options.Sources.Count);
		Assert.Equal(expectedRoutes[0].Url, options.Sources[0].Url);
		Assert.Equal(expectedRoutes[1].Url, options.Sources[1].Url);
	}

	[Fact]
	public void AddKoalesce_WhenKoalesceSectionIsMissing_ShouldThrowKoalesceConfigurationNotFoundException()
	{
		// Arrange: Empty configuration (simulating appsettings.json without "Koalesce" section)
		// Podemos usar um objeto vazio ou com outra seção irrelevante
		var appSettingsStub = new { OtherSection = "Irrelevant" };
		var configuration = ConfigurationHelper
			.BuildConfigurationFromObject(appSettingsStub);

		// Act & Assert: Expect custom exception when attempting to use Koalesce
		Assert.Throws<KoalesceConfigurationNotFoundException>(() =>
			Services.AddKoalesce(configuration)
				.AddProvider<DummyProvider, KoalesceOptions>());
	}

	[Fact]
	public void Koalesce_WhenOpenApiSourcesContainInvalidSourceUrl_ShouldThrowValidationException()
	{
		// Arrange
		var appSettingsStub = new
		{
			Koalesce = new KoalesceOptions
			{
				MergedDocumentPath = "/v1/mergedapidefinition.json",
				Sources = new List<SourceDefinition>
				{
					new SourceDefinition { Url = "https://api1.com/v1/apidefinition.json" },
					new SourceDefinition { Url = "localhost:8002/v1/apidefinition.json" } // Invalid URL (missing scheme)
                }
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
			var options = provider.GetRequiredService<IOptions<KoalesceOpenApiOptions>>().Value;
		});

		Assert.Contains("must be a valid absolute URL", exception.Message);
		Assert.Contains("index 1", exception.Message);
	}
}