namespace Koalesce.Tests.Unit;

public class KoalesceCacheOptionsTests : KoalesceUnitTestBase
{
	[Fact]
	public void KoalesceCache_WhenConfigured_ShouldBindCacheOptions()
	{
		// Arrange
		var config = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				{ "Koalesce:Cache:AbsoluteExpirationSeconds", "86400" }, // 24h
                { "Koalesce:Cache:SlidingExpirationSeconds", "300" }, // 5 min
                { "Koalesce:Cache:MinExpirationSeconds", "30" } // 30 sec
            })
			.Build();

		Services.Configure<KoalesceOptions>(config.GetSection(KoalesceOptions.ConfigurationSectionName));
		var provider = Services.BuildServiceProvider();
		var options = provider.GetService<IOptions<KoalesceOptions>>()?.Value;

		// Act & Assert
		Assert.NotNull(options);
		Assert.NotNull(options.Cache);
		Assert.Equal(86400, options.Cache.AbsoluteExpirationSeconds);
		Assert.Equal(300, options.Cache.SlidingExpirationSeconds);
		Assert.Equal(30, options.Cache.MinExpirationSeconds);
	}

	[Fact]
	public void KoalesceCache_WhenAbsoluteExpirationTooShort_ShouldThrowException()
	{
		// Arrange
		var options = new KoalesceOptions
		{
			SourceOpenApiUrls = ["http://fakeapi.com/v1/apidefinition.json"],
			MergedOpenApiPath = "/swagger/v1/swagger.json",
			Cache = new KoalesceCacheOptions
			{
				AbsoluteExpirationSeconds = 10, // Invalid (Below MinExpirationSeconds)
				MinExpirationSeconds = 30
			}
		};

		// Act & Assert
		var ex = Assert.Throws<KoalesceInvalidConfigurationValuesException>(() =>
			options.Validate());

		Assert.True(ex.Message.Length > 0);
	}

	[Fact]
	public void KoalesceCache_WhenSlidingExpirationExceedsAbsolute_ShouldThrowException()
	{
		// Arrange
		var options = new KoalesceOptions
		{
			SourceOpenApiUrls = ["http://fakeapi.com/v1/apidefinition.json"],
			MergedOpenApiPath = "/swagger/v1/swagger.json",
			Cache = new KoalesceCacheOptions
			{
				AbsoluteExpirationSeconds = 600,
				SlidingExpirationSeconds = 700 // Invalid (Exceeds Absolute Expiration)
			}
		};

		// Act & Assert
		var ex = Assert.Throws<KoalesceInvalidConfigurationValuesException>(() =>
			options.Validate());

		Assert.True(ex.Message.Length > 0);
	}

	[Fact]
	public async Task KoalesceMiddleware_WhenCacheEnabled_ShouldCacheMergedDocument()
	{
		// Arrange
		string mergedOpenApiPath = "/swagger/v1/swagger.json";
		var cache = new MemoryCache(new MemoryCacheOptions());
		var options = Options.Create(new KoalesceOptions
		{
			MergedOpenApiPath = mergedOpenApiPath,
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

		var context = CreateHttpContext(mergedOpenApiPath);

		// Act
		await middleware.InvokeAsync(context);

		// Assert
		Assert.True(cache.TryGetValue(mergedOpenApiPath, out string cachedDocument));
		Assert.False(string.IsNullOrWhiteSpace(cachedDocument));
	}

	[Fact]
	public async Task KoalesceMiddleware_WhenCacheDisabled_ShouldNotCache()
	{
		// Arrange
		string mergedOpenApiPath = "/swagger/v1/swagger.json";
		var cache = new MemoryCache(new MemoryCacheOptions());
		var options = Options.Create(new KoalesceOptions
		{
			MergedOpenApiPath = mergedOpenApiPath,
			Cache = new KoalesceCacheOptions { DisableCache = true }
		});

		var provider = new DummyProvider();
		var logger = CreateLogger<KoalesceMiddleware>();

		var middleware = new KoalesceMiddleware(
			options, logger, provider, context => Task.CompletedTask, cache);

		var context = CreateHttpContext(mergedOpenApiPath);

		// Act
		await middleware.InvokeAsync(context);

		// Assert
		Assert.False(cache.TryGetValue(mergedOpenApiPath, out _));
	}
}
