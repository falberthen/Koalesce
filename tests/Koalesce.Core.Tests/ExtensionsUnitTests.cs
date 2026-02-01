namespace Koalesce.Core.Tests;

[Collection("Koalesce Core Extensions Unit Tests")]
public class ExtensionsUnitTests : KoalesceUnitTestBase
{
	[Fact]
	public void Koalesce_WhenServicesAndConfigurationProvided_ShouldRegisterDependenciesAndBindOptions()
	{
		// Arrange
		var appSettingsStub = new
		{
			Koalesce = new CoreOptions
			{
				MergedEndpoint = "/v1/mergedapidefinition.json",
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
		var optionsWrapper = provider.GetService<IOptions<CoreOptions>>();
		Assert.NotNull(optionsWrapper);

		var options = optionsWrapper.Value;

		Assert.Equal("/v1/mergedapidefinition.json", options.MergedEndpoint);
		Assert.Equal(2, options.Sources.Count);
		Assert.Equal("https://api1.com/v1/apidefinition.json", options.Sources[0].Url);
		Assert.Equal("https://api2.com/v1/apidefinition.json", options.Sources[1].Url);
	}

	[Fact]
	public void Koalesce_ShouldRegisterMergeService()
	{
		// Arrange
		var appSettingsStub = new
		{
			Koalesce = new CoreOptions
			{
				MergedEndpoint = "/v1/mergedapidefinition.json",
				Sources = new List<ApiSource>
				{
					new ApiSource { Url = "https://api1.com/v1/apidefinition.json" }
				}
			}
		};

		var configuration = ConfigurationHelper
			.BuildConfigurationFromObject(appSettingsStub);

		Services.AddKoalesce(configuration);

		var provider = Services.BuildServiceProvider();

		// Act
		var mergeService = provider.GetService<IKoalesceMergeService>();

		// Assert
		Assert.NotNull(mergeService);
	}

	[Fact]
	public void Koalesce_ShouldRegisterMemoryCache()
	{
		// Arrange
		var appSettingsStub = new
		{
			Koalesce = new CoreOptions
			{
				MergedEndpoint = "/v1/mergedapidefinition.json",
				Sources = new List<ApiSource>
				{
					new ApiSource { Url = "https://api1.com/v1/apidefinition.json" }
				}
			}
		};

		var configuration = ConfigurationHelper
			.BuildConfigurationFromObject(appSettingsStub);

		Services.AddKoalesce(configuration);

		var provider = Services.BuildServiceProvider();

		// Assert
		Assert.NotNull(provider.GetService<IMemoryCache>());
	}

	[Fact]
	public void Koalesce_WhenServicesIsNull_ShouldThrowArgumentNullException()
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
	public void Koalesce_WhenConfigurationIsNull_ShouldThrowArgumentNullException()
	{
		// Act & Assert
		var exception = Assert.Throws<ArgumentNullException>(() =>
			Services.AddKoalesce(null!));

		Assert.Equal("configuration", exception.ParamName);
	}
}