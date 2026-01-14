namespace Koalesce.Tests.Unit;

[Collection("Koalesce Core Cache Options Unit Tests")]
public class KoalesceCoreCacheOptionsTests : KoalesceUnitTestBase
{
	[Fact]
	public void KoalesceCache_WhenConfigured_ShouldBindCacheOptions()
	{
		// Arrange
		var appSettingsStub = new
		{
			Koalesce = new KoalesceOptions
			{
				Cache = new KoalesceCacheOptions
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

		Services.Configure<KoalesceOptions>(config.GetSection(KoalesceOptions.ConfigurationSectionName));
		var provider = Services.BuildServiceProvider();

		// Act
		var options = provider.GetService<IOptions<KoalesceOptions>>()?.Value;

		// Assert
		Assert.NotNull(options);
		Assert.NotNull(options.Cache);

		// Verify values are correctly bound from the typed object configuration
		Assert.Equal(86400, options.Cache.AbsoluteExpirationSeconds);
		Assert.Equal(300, options.Cache.SlidingExpirationSeconds);
		Assert.Equal(60, options.Cache.MinExpirationSeconds);
	}

	[Fact]
	public void KoalesceCache_WhenAbsoluteExpirationBelowDefaultMin_ShouldThrowException()
	{
		// Arrange
		var options = new KoalesceOptions
		{
			Sources = new List<SourceDefinition>
			{
				new SourceDefinition
				{
					Url = "http://fakeapi.com/v1/apidefinition.json"
				}
			},
			MergedDocumentPath = "/mergedapidefinition.json",
			Cache = new KoalesceCacheOptions
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
	public void KoalesceCache_WhenAbsoluteExpirationBelowCustomMin_ShouldThrowException()
	{
		// Arrange
		var options = new KoalesceOptions
		{
			Sources = new List<SourceDefinition> 
			{
				new SourceDefinition
				{
					Url = "http://fakeapi.com/v1/apidefinition.json",
				}
			},
			MergedDocumentPath = "/mergedapidefinition.json",
			Cache = new KoalesceCacheOptions
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
	public void KoalesceCache_WhenSlidingExpirationExceedsAbsolute_ShouldThrowException()
	{
		// Arrange
		var options = new KoalesceOptions
		{
			Sources = new List<SourceDefinition> 
			{
				new SourceDefinition
				{
					Url = "http://fakeapi.com/v1/apidefinition.json",
				}
			},
			MergedDocumentPath = "/mergedapidefinition.json",
			Cache = new KoalesceCacheOptions
			{
				AbsoluteExpirationSeconds = 600,
				SlidingExpirationSeconds = 700 // Logical error
			}
		};

		// Act & Assert
		Assert.Throws<KoalesceInvalidConfigurationValuesException>(() => 
			options.Validate());
	}

	[Fact]
	public async Task KoalesceMiddleware_WhenCacheEnabled_ShouldCacheMergedDocument()
	{
		// Arrange
		string mergedDocumentPath = "/mergedapidefinition.json";
		var cache = new MemoryCache(new MemoryCacheOptions());
		var options = Options.Create(new KoalesceOptions
		{
			MergedDocumentPath = mergedDocumentPath,
			Cache = new KoalesceCacheOptions
			{
				DisableCache = false,
				AbsoluteExpirationSeconds = 10,
				SlidingExpirationSeconds = 5
			}
		});

		var provider = new DummyProvider(); // Simulated API Merge
		var logger = CreateLogger<KoalesceMiddleware>();

		var middleware = new KoalesceMiddleware(
			options, logger, provider, context => Task.CompletedTask, cache);

		var context = CreateHttpContext(mergedDocumentPath);

		// Act
		await middleware.InvokeAsync(context);

		// Assert
		Assert.True(cache.TryGetValue(mergedDocumentPath, out string cachedDocument));
		Assert.False(string.IsNullOrWhiteSpace(cachedDocument));
	}

	[Fact]
	public async Task KoalesceMiddleware_WhenCacheDisabled_ShouldNotCache()
	{
		// Arrange
		string mergedDocumentPath = "/mergedapidefinition.json";
		var cache = new MemoryCache(new MemoryCacheOptions());
		var options = Options.Create(new KoalesceOptions
		{
			MergedDocumentPath = mergedDocumentPath,
			Cache = new KoalesceCacheOptions { DisableCache = true }
		});

		var provider = new DummyProvider();
		var logger = CreateLogger<KoalesceMiddleware>();

		var middleware = new KoalesceMiddleware(
			options, logger, provider, context => Task.CompletedTask, cache);

		var context = CreateHttpContext(mergedDocumentPath);

		// Act
		await middleware.InvokeAsync(context);

		// Assert
		Assert.False(cache.TryGetValue(mergedDocumentPath, out _));
	}
}