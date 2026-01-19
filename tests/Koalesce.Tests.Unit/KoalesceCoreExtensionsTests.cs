namespace Koalesce.Tests.Unit;

[Collection("Koalesce Core Extension Unit Tests")]
public class KoalesceCoreExtensionsTests : KoalesceUnitTestBase
{
	[Fact]
	public void AddKoalesce_WhenServicesAndConfigurationProvided_ShouldRegisterDependenciesAndBindOptions()
	{
		// Arrange		
		var appSettingsStub = new
		{
			Koalesce = new KoalesceOptions
			{
				MergedDocumentPath = "/v1/mergedapidefinition.json",
				Sources = new List<ApiSource>
				{
					new ApiSource { Url = "https://api1.com/v1/apidefinition.json" },
					new ApiSource { Url = "https://api2.com/v1/apidefinition.json" }
				}
			}
		};

		var configuration = ConfigurationHelper
			.BuildConfigurationFromObject(appSettingsStub);

		// Act
		Services.AddKoalesce(configuration);

		var provider = Services.BuildServiceProvider();

		// Assert
		// Checks for builder registry
		Assert.NotNull(provider.GetService<IKoalesceBuilder>());

		// Getting the wrapper
		var optionsWrapper = provider.GetService<IOptions<KoalesceOptions>>();
		Assert.NotNull(optionsWrapper);

		// Forcing binding and validation via .Value
		var options = optionsWrapper.Value;

		// Validating bound values
		Assert.Equal("/v1/mergedapidefinition.json", options.MergedDocumentPath);
		Assert.Equal(2, options.Sources.Count);
		Assert.Equal("https://api1.com/v1/apidefinition.json", options.Sources[0].Url);
		Assert.Equal("https://api2.com/v1/apidefinition.json", options.Sources[1].Url);
	}

	[Fact]
	public void AddKoalesce_ShouldRegisterSingletonKoalesceBuilder()
	{
		// Arrange
		var appSettingsStub = new
		{
			Koalesce = new KoalesceOptions
			{
				MergedDocumentPath = "/v1/mergedapidefinition.json",
				Sources = new List<ApiSource>
				{
					new ApiSource { Url = "https://api1.com/v1/apidefinition.json" }
				}
			}
		};

		var configuration = ConfigurationHelper
			.BuildConfigurationFromObject(appSettingsStub);

		Services.AddKoalesce(configuration)
			.AddProvider<DummyProvider, KoalesceOptions>();

		var provider = Services.BuildServiceProvider();

		// Act
		var builder1 = provider.GetService<IKoalesceBuilder>();
		var builder2 = provider.GetService<IKoalesceBuilder>();

		// Assert
		Assert.NotNull(builder1);
		Assert.Same(builder1, builder2); // Singleton instance
	}

	[Fact]
	public void UseKoalesce_WhenAddProvider_ShouldEnableMiddleware()
	{
		// Arrange
		var appSettingsStub = new
		{
			Koalesce = new KoalesceOptions
			{
				MergedDocumentPath = "/v1/mergedapidefinition.json",
				Sources = new List<ApiSource>
				{
					new ApiSource { Url = "https://api1.com/v1/apidefinition.json" }
				}
			}
		};

		var configuration = ConfigurationHelper
			.BuildConfigurationFromObject(appSettingsStub);

		var builder = Services.AddKoalesce(configuration)
			.AddProvider<DummyProvider, DummyOptions>();

		var provider = builder.Services.BuildServiceProvider();
		var app = new FakeApplicationBuilder(provider);

		// Act
		app.UseKoalesce();

		// Assert
		Assert.True(app.MiddlewareRegistered);
	}

	[Fact]
	public void AddKoalesce_WhenServicesIsNull_ShouldThrowArgumentNullException()
	{
		// Arrange
		IServiceCollection? nullServices = null;
		var configuration = new ConfigurationBuilder().Build();

		// Act & Assert
		var exception = Assert.Throws<ArgumentNullException>(() =>
			nullServices!.AddKoalesce(configuration));

		Assert.Equal("services", exception.ParamName);
	}

	[Fact]
	public void AddKoalesce_WhenConfigurationIsNull_ShouldThrowArgumentNullException()
	{
		// Act & Assert
		var exception = Assert.Throws<ArgumentNullException>(() =>
			Services.AddKoalesce(null!));

		Assert.Equal("configuration", exception.ParamName);
	}
}