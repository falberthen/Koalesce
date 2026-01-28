namespace Koalesce.Core;

/// <summary>
/// Middleware to handle API requests
/// </summary>
public class KoalesceMiddleware
{
	private readonly string _mergedDocumentPath;
	private readonly IKoalesceProvider _koalesceProvider;
	private readonly ILogger<KoalesceMiddleware> _logger;
	private readonly RequestDelegate _next;

	// Caching	
	private readonly IMemoryCache _cache;
	private readonly KoalesceCacheOptions _cacheOptions;

	public KoalesceMiddleware(
		IOptions<KoalesceOptions> options,
		ILogger<KoalesceMiddleware> logger,
		IKoalesceProvider koalesceProvider,
		RequestDelegate next,
		IMemoryCache cache)
	{
		ArgumentNullException.ThrowIfNull(koalesceProvider);
		ArgumentNullException.ThrowIfNull(logger);
		ArgumentNullException.ThrowIfNull(next);
		ArgumentNullException.ThrowIfNull(cache);

		_koalesceProvider = koalesceProvider;
		_logger = logger;
		_next = next;
		_cache = cache;

		var opts = options.Value;
		_cacheOptions = opts.Cache;
		_mergedDocumentPath = opts.MergedDocumentPath;
	}

	/// <summary>
	/// Middleware invocation logic.
	/// </summary>
	public async Task InvokeAsync(HttpContext context)
	{
		// Only log the generic request path if Debug level is actually enabled
		// This prevents "spamming" logs for every single API request in Production
		if (_logger.IsEnabled(LogLevel.Debug))
			_logger.LogDebug("Koalesce Middleware inspecting path: {RequestPath}", context.Request.Path);

		// If the request path matches the expected merged merged path
		if (context.Request.Path.Equals(_mergedDocumentPath, StringComparison.OrdinalIgnoreCase))
		{
			_logger.LogInformation("Handling Koalesced path request: {MergePath}", _mergedDocumentPath);

			// If cache is disabled, always recompute
			if (_cacheOptions.DisableCache)
			{
				_logger.LogInformation("Cache is disabled. Always recomputing Koalesced document.");
				await RecomputeAndRespondAsync(context);
				return;
			}

			// Try fetching from cache first
			if (_cache.TryGetValue(_mergedDocumentPath, out string? cachedDocument) && !string.IsNullOrEmpty(cachedDocument))
			{
				_logger.LogInformation("Returning cached Koalesced document.");
				context.Response.ContentType = "application/json";
				await context.Response.WriteAsync(cachedDocument);
				return;
			}

			_logger.LogInformation("Cache expired or not found. Rebuilding Koalesced document...");
			await RecomputeAndRespondAsync(context);
		}
		else
		{
			// Not a Koalesce request, pass to next middleware
			await _next(context);
		}
	}

	/// <summary>
	/// Recomputes OpenAPI document and stores it in cache (if enabled)
	/// </summary>
	private async Task RecomputeAndRespondAsync(HttpContext context)
	{
		try
		{
			// Calling provider to merge definitions
			string mergedDocument = await _koalesceProvider.ProvideMergedDocumentAsync();

			if (string.IsNullOrWhiteSpace(mergedDocument))
			{
				context.Response.StatusCode = StatusCodes.Status404NotFound;
				await context.Response.WriteAsync("No APIs available to Koalesce.");
				return;
			}

			// Store result in cache if caching is enabled
			if (!_cacheOptions.DisableCache)
			{
				// Clamp expiration times to the configured minimum safety floor
				var safeAbsoluteExpiration = Math.Max(
					_cacheOptions.AbsoluteExpirationSeconds,
					_cacheOptions.MinExpirationSeconds
				);

				var safeSlidingExpiration = Math.Max(
					_cacheOptions.SlidingExpirationSeconds,
					_cacheOptions.MinExpirationSeconds
				);

				// Setting cache entry
				_cache.Set(_mergedDocumentPath, mergedDocument,
				new MemoryCacheEntryOptions()
					.SetAbsoluteExpiration(TimeSpan.FromSeconds(safeAbsoluteExpiration))
					.SetSlidingExpiration(TimeSpan.FromSeconds(safeSlidingExpiration))
				);
			}

			context.Response.ContentType = "application/json";
			await context.Response.WriteAsync(mergedDocument);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to generate Koalesce document.");
			context.Response.StatusCode = StatusCodes.Status500InternalServerError;
			await context.Response.WriteAsync(ex.Message);
		}
	}
}