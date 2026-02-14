namespace Koalesce.Core.Tests;

[Collection("Koalesce Core Middleware Cache Unit Tests")]
public class MiddlewareCacheUnitTests : KoalesceUnitTestBase
{
	private const string _MergedEndpoint = "/mergedapidefinition.json";

	[Fact]
	public async Task Koalesce_WhenCacheEnabled_ShouldCacheMergedDocument()
	{
		// Arrange
		string MergedEndpoint = "/mergedapidefinition.json";
		var cache = new MemoryCache(new MemoryCacheOptions());
		var options = Microsoft.Extensions.Options.Options.Create(new CoreOptions
		{
			MergedEndpoint = MergedEndpoint,
			Cache = new CacheOptions
			{
				DisableCache = false,
				AbsoluteExpirationSeconds = 10,
				SlidingExpirationSeconds = 5
			}
		});

		var mergeService = new DummyMergeService();
		var logger = CreateLogger<KoalesceMiddleware>();

		var middleware = new KoalesceMiddleware(
			options, logger, mergeService, context => Task.CompletedTask, cache);

		var context = CreateHttpContext(MergedEndpoint);

		// Act
		await middleware.InvokeAsync(context);

		// Assert
		Assert.True(cache.TryGetValue(MergedEndpoint, out string? cachedDocument));
		Assert.False(string.IsNullOrWhiteSpace(cachedDocument));
	}

	[Fact]
	public async Task Koalesce_WhenCacheDisabled_ShouldNotCache()
	{
		// Arrange
		string MergedEndpoint = "/mergedapidefinition.json";
		var cache = new MemoryCache(new MemoryCacheOptions());
		var options = Microsoft.Extensions.Options.Options.Create(new CoreOptions
		{
			MergedEndpoint = MergedEndpoint,
			Cache = new CacheOptions { DisableCache = true }
		});

		var mergeService = new DummyMergeService();
		var logger = CreateLogger<KoalesceMiddleware>();

		var middleware = new KoalesceMiddleware(
			options, logger, mergeService, context => Task.CompletedTask, cache);

		var context = CreateHttpContext(MergedEndpoint);

		// Act
		await middleware.InvokeAsync(context);

		// Assert
		Assert.False(cache.TryGetValue(MergedEndpoint, out _));
	}

	[Fact]
	public async Task Koalesce_WhenDisableCacheIsTrue_ShouldAlwaysCallMergeService()
	{
		// Arrange
		var mergeService = new DummyMergeService();
		var options = new CacheOptions { DisableCache = true };

		var serviceProvider = BuildServiceProvider(options, mergeService);
		var middleware = CreateMiddlewareFromContainer(serviceProvider);

		var cache = serviceProvider.GetRequiredService<IMemoryCache>();
		var context = new DefaultHttpContext();
		context.Request.Path = _MergedEndpoint;

		// Act
		await middleware.InvokeAsync(context); // 1st call
		await middleware.InvokeAsync(context); // 2nd call

		// Assert
		Assert.Equal(2, mergeService.CallCount); // Service called twice
		Assert.False(cache.TryGetValue(_MergedEndpoint, out _)); // Cache empty
	}

	[Fact]
	public async Task Koalesce_WhenCacheEnabled_ShouldRespectCacheHit()
	{
		// Arrange
		var mergeService = new DummyMergeService();

		var options = new CacheOptions
		{
			DisableCache = false,
			AbsoluteExpirationSeconds = 60,
			SlidingExpirationSeconds = 30
		};

		var serviceProvider = BuildServiceProvider(options, mergeService);
		var middleware = CreateMiddlewareFromContainer(serviceProvider);
		var cache = serviceProvider.GetRequiredService<IMemoryCache>();

		var context = new DefaultHttpContext();
		context.Request.Path = _MergedEndpoint;

		// Act
		await middleware.InvokeAsync(context); // 1st call (Miss)
		await middleware.InvokeAsync(context); // 2nd call (Hit)

		// Assert
		Assert.Equal(1, mergeService.CallCount); // Service called once
		Assert.True(cache.TryGetValue(_MergedEndpoint, out _)); // Item in cache
	}

	[Fact]
	public async Task Koalesce_WhenSlidingExpiration_ShouldExtendLifetime()
	{
		// Arrange
		var mergeService = new DummyMergeService();
		var options = new CacheOptions
		{
			DisableCache = false,
			AbsoluteExpirationSeconds = 10,
			SlidingExpirationSeconds = 2,
			MinExpirationSeconds = 0
		};

		var serviceProvider = BuildServiceProvider(options, mergeService);
		var middleware = CreateMiddlewareFromContainer(serviceProvider);

		var context = new DefaultHttpContext();
		context.Request.Path = _MergedEndpoint;

		// Act 1: Initial Call
		await middleware.InvokeAsync(context);

		// Wait 1.5s (< 2s)
		await Task.Delay(1500);

		// Act 2: Access again (Resets timer)
		await middleware.InvokeAsync(context);
		Assert.Equal(1, mergeService.CallCount); // Hit confirmed

		// Wait another 1.5s (Total 3s > 2s original sliding)
		await Task.Delay(1500);

		// Act 3: Access again
		await middleware.InvokeAsync(context);

		// Assert
		Assert.Equal(1, mergeService.CallCount); // Still hit (Sliding worked)
	}

	private IServiceProvider BuildServiceProvider(CacheOptions cacheOptions, DummyMergeService mergeService)
	{
		// Create a fresh ServiceCollection for each test to avoid cross-test pollution
		var services = new ServiceCollection();

		// Create configuration structure using anonymous objects + typed cache options
		var appSettingsStub = new
		{
			Koalesce = new
			{
				MergedEndpoint = _MergedEndpoint,
				Sources = new[]
				{
					new { Url = "https://api1.com/v1/apidefinition.json" },
					new { Url = "https://api2.com/v1/apidefinition.json" }
				},
				Cache = cacheOptions
			}
		};

		var configuration = ConfigurationHelper.BuildConfigurationFromObject(appSettingsStub);

		// Register Services
		services.AddLogging();
		services.AddKoalesce(configuration);

		// Swap the merge service for our spy instance
		services.RemoveAll<IKoalesceMergeService>();
		services.AddSingleton<IKoalesceMergeService>(mergeService);

		return services.BuildServiceProvider();
	}

	// Helper to instantiate middleware using the DI container
	private KoalesceMiddleware CreateMiddlewareFromContainer(IServiceProvider provider)
	{
		return ActivatorUtilities.CreateInstance<KoalesceMiddleware>(
			provider,
			(RequestDelegate)(context => Task.CompletedTask)
		);
	}

	/// <summary>
	/// Dummy implementation of IKoalesceMergeService for testing
	/// </summary>
	private class DummyMergeService : IKoalesceMergeService
	{
		public int CallCount { get; private set; }

		public Task<MergeResult> MergeSpecificationsAsync(string? outputPath = null)
		{
			CallCount++;
			return Task.FromResult(new MergeResult(
				"{\"openapi\":\"3.0.1\",\"info\":{\"title\":\"Merged API\",\"version\":\"1.0\"}}",
				[])
			);
		}
	}
}