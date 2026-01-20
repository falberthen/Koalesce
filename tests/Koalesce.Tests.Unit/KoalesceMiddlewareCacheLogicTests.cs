namespace Koalesce.Tests.Unit;

[Collection("Koalesce Core Middleware Cache Unit Tests")]
public class KoalesceMiddlewareCacheTests : KoalesceUnitTestBase
{
	private const string _mergedDocumentPath = "/mergedapidefinition.json";

	[Fact]
	public async Task Koalesce_WhenCacheEnabled_ShouldCacheMergedDocument()
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
	public async Task Koalesce_WhenCacheDisabled_ShouldNotCache()
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

	[Fact]
	public async Task Koalesce_WhenDisableCacheIsTrue_ShouldAlwaysCallProvider()
	{
		// Arrange
		var dummyProvider = new DummyProvider();
		var options = new KoalesceCacheOptions { DisableCache = true };

		var serviceProvider = BuildServiceProvider(options, dummyProvider);
		var middleware = CreateMiddlewareFromContainer(serviceProvider);

		var cache = serviceProvider.GetRequiredService<IMemoryCache>();
		var context = new DefaultHttpContext();
		context.Request.Path = _mergedDocumentPath;

		// Act
		await middleware.InvokeAsync(context); // 1st call
		await middleware.InvokeAsync(context); // 2nd call

		// Assert
		Assert.Equal(2, dummyProvider.CallCount); // Provider called twice
		Assert.False(cache.TryGetValue(_mergedDocumentPath, out _)); // Cache empty
	}

	[Fact]
	public async Task Koalesce_WhenCacheEnabled_ShouldRespectCacheHit()
	{
		// Arrange
		var dummyProvider = new DummyProvider();

		var options = new KoalesceCacheOptions
		{
			DisableCache = false,
			AbsoluteExpirationSeconds = 60,
			SlidingExpirationSeconds = 30
		};

		var serviceProvider = BuildServiceProvider(options, dummyProvider);
		var middleware = CreateMiddlewareFromContainer(serviceProvider);
		var cache = serviceProvider.GetRequiredService<IMemoryCache>();

		var context = new DefaultHttpContext();
		context.Request.Path = _mergedDocumentPath;

		// Act
		await middleware.InvokeAsync(context); // 1st call (Miss)
		await middleware.InvokeAsync(context); // 2nd call (Hit)

		// Assert
		Assert.Equal(1, dummyProvider.CallCount); // Provider called once
		Assert.True(cache.TryGetValue(_mergedDocumentPath, out _)); // Item in cache
	}

	[Fact]
	public async Task Koalesce_WhenSlidingExpiration_ShouldExtendLifetime()
	{
		// Arrange
		var dummyProvider = new DummyProvider();
		var options = new KoalesceCacheOptions
		{
			DisableCache = false,
			AbsoluteExpirationSeconds = 10,
			SlidingExpirationSeconds = 2,
			MinExpirationSeconds = 0
		};

		var serviceProvider = BuildServiceProvider(options, dummyProvider);
		var middleware = CreateMiddlewareFromContainer(serviceProvider);

		var context = new DefaultHttpContext();
		context.Request.Path = _mergedDocumentPath;

		// Act 1: Initial Call
		await middleware.InvokeAsync(context);

		// Wait 1.5s (< 2s) 
		await Task.Delay(1500);

		// Act 2: Access again (Resets timer)
		await middleware.InvokeAsync(context);
		Assert.Equal(1, dummyProvider.CallCount); // Hit confirmed

		// Wait another 1.5s (Total 3s > 2s original sliding)
		await Task.Delay(1500);

		// Act 3: Access again
		await middleware.InvokeAsync(context);

		// Assert
		Assert.Equal(1, dummyProvider.CallCount); // Still hit (Sliding worked)
	}

	private IServiceProvider BuildServiceProvider(KoalesceCacheOptions cacheOptions, DummyProvider dummyProvider)
	{
		// Create configuration structure using anonymous objects + typed cache options
		var appSettingsStub = new
		{
			Koalesce = new
			{
				MergedDocumentPath = _mergedDocumentPath,
				Sources = new[]
				{
					new { Url = "https://api1.com/v1/apidefinition.json" },
					new { Url = "https://api2.com/v1/apidefinition.json" }
				},
				// Pass the typed options object directly. JsonSerializer will serialize 
				// its properties (AbsoluteExpirationSeconds, etc.) correctly into the JSON stream.
				Cache = cacheOptions
			}
		};

		var configuration = ConfigurationHelper.BuildConfigurationFromObject(appSettingsStub);

		// Register Services (Standard DI Pattern)
		Services.AddLogging(); // Required by Middleware
		Services.AddKoalesce(configuration)
			.AddProvider<DummyProvider, KoalesceOptions>();

		// Swap the provider for our spy instance
		Services.RemoveAll<IKoalesceProvider>();
		Services.AddSingleton<IKoalesceProvider>(dummyProvider);

		return Services.BuildServiceProvider();
	}

	// Helper to instantiate middleware using the DI container
	private KoalesceMiddleware CreateMiddlewareFromContainer(IServiceProvider provider)
	{
		return ActivatorUtilities.CreateInstance<KoalesceMiddleware>(
			provider,
			(RequestDelegate)(context => Task.CompletedTask)
		);
	}
}