namespace Koalesce.Core.Tests;

[Collection("Koalesce Cache Options Unit Tests")]
public class CacheOptionsUnitTests : KoalesceUnitTestBase
{
	[Fact]
	public void Koalesce_WhenCacheConfigured_ShouldBindCacheOptions()
	{
		// Arrange
		var appSettingsStub = new
		{
			Koalesce = new CoreOptions
			{
				Cache = new CacheOptions
				{
					AbsoluteExpirationSeconds = 86400,
					SlidingExpirationSeconds = 300,
					MinExpirationSeconds = 60
				}
			}
		};

		// Serialize to JSON and load into ConfigurationBuilder via stream
		var jsonString = JsonSerializer.Serialize(appSettingsStub);
		var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(jsonString));

		var config = new ConfigurationBuilder()
			.AddJsonStream(memoryStream)
			.Build();

		Services.Configure<CoreOptions>(config.GetSection(CoreOptions.ConfigurationSectionName));
		var provider = Services.BuildServiceProvider();

		// Act
		var options = provider.GetService<IOptions<CoreOptions>>()?.Value;

		// Assert
		Assert.NotNull(options);
		Assert.NotNull(options.Cache);

		// Verify values are correctly bound from the typed object configuration
		Assert.Equal(86400, options.Cache.AbsoluteExpirationSeconds);
		Assert.Equal(300, options.Cache.SlidingExpirationSeconds);
		Assert.Equal(60, options.Cache.MinExpirationSeconds);
	}

	[Fact]
	public void Koalesce_WhenCacheHasAbsoluteExpirationBelowDefaultMin_ShouldThrowException()
	{
		// Arrange
		var options = new CoreOptions
		{
			Sources = new List<ApiSource>
			{
				new ApiSource
				{
					Url = "http://fakeapi.com/v1/apidefinition.json"
				}
			},
			MergedEndpoint = "/mergedapidefinition.json",
			Cache = new CacheOptions
			{
				AbsoluteExpirationSeconds = 10, // < 30 (Default)
												// Implicit MinExpirationSeconds is default value
			}
		};

		// Act & Assert
		Assert.Throws<KoalesceInvalidConfigurationValuesException>(() => 
			options.Validate());
	}

	[Fact]
	public void Koalesce_WhenCacheHasAbsoluteExpirationBelowCustomMin_ShouldThrowException()
	{
		// Arrange
		var options = new CoreOptions
		{
			Sources = new List<ApiSource> 
			{
				new ApiSource
				{
					Url = "http://fakeapi.com/v1/apidefinition.json",
				}
			},
			MergedEndpoint = "/mergedapidefinition.json",
			Cache = new CacheOptions
			{
				MinExpirationSeconds = 600,
				AbsoluteExpirationSeconds = 300  // Logical error
			}
		};

		// Act & Assert
		Assert.Throws<KoalesceInvalidConfigurationValuesException>(() => 
			options.Validate());
	}

	[Fact]
	public void Koalesce_WhenCacheHasSlidingExpirationExceedsAbsolute_ShouldThrowException()
	{
		// Arrange
		var options = new CoreOptions
		{
			Sources = new List<ApiSource> 
			{
				new ApiSource
				{
					Url = "http://fakeapi.com/v1/apidefinition.json",
				}
			},
			MergedEndpoint = "/mergedapidefinition.json",
			Cache = new CacheOptions
			{
				AbsoluteExpirationSeconds = 600,
				SlidingExpirationSeconds = 700 // Logical error
			}
		};

		// Act & Assert
		Assert.Throws<KoalesceInvalidConfigurationValuesException>(() => 
			options.Validate());
	}
}