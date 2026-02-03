namespace Koalesce.Core;

/// <summary>
/// Middleware to handle API requests
/// </summary>
public class KoalesceMiddleware
{
	private readonly string? _mergedEndpoint;
	private readonly IKoalesceMergeService _mergeService;
	private readonly ILogger<KoalesceMiddleware> _logger;
	private readonly RequestDelegate _next;

	// Caching
	private readonly IMemoryCache _cache;
	private readonly CacheOptions _cacheOptions;

	public KoalesceMiddleware(
		IOptions<CoreOptions> options,
		ILogger<KoalesceMiddleware> logger,
		IKoalesceMergeService mergeService,
		RequestDelegate next,
		IMemoryCache cache)
	{
		ArgumentNullException.ThrowIfNull(mergeService);
		ArgumentNullException.ThrowIfNull(logger);
		ArgumentNullException.ThrowIfNull(next);
		ArgumentNullException.ThrowIfNull(cache);

		_mergeService = mergeService;
		_logger = logger;
		_next = next;
		_cache = cache;

		var opts = options.Value;
		_cacheOptions = opts.Cache;
		_mergedEndpoint = opts.MergedEndpoint;
	}

	/// <summary>
	/// Middleware invocation logic.
	/// </summary>
	public async Task InvokeAsync(HttpContext context)
	{
		// When using the middleware, MergedEndpoint must be configured
		if (string.IsNullOrEmpty(_mergedEndpoint))	
			throw new KoalesceInvalidConfigurationValuesException(CoreConstants.MergedEndpointCannotBeEmpty);

		// Only log the generic request path if Debug level is actually enabled
		// This prevents "spamming" logs for every single API request in Production
		if (_logger.IsEnabled(LogLevel.Debug))
			_logger.LogDebug("Koalesce Middleware inspecting path: {RequestPath}", context.Request.Path);

		// If the request path matches the expected merged merged path
		if (context.Request.Path.Equals(_mergedEndpoint, StringComparison.OrdinalIgnoreCase))
		{
			_logger.LogInformation("Handling Koalesced path request: {MergePath}", _mergedEndpoint);

			// If cache is disabled, always recompute
			if (_cacheOptions.DisableCache)
			{
				_logger.LogInformation("Cache is disabled. Always recomputing Koalesced document.");
				await RecomputeAndRespondAsync(context);
				return;
			}

			// Try fetching from cache first
			if (_cache.TryGetValue(_mergedEndpoint, out string? cachedDocument) && !string.IsNullOrEmpty(cachedDocument))
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
			// Calling merge service
			string mergedDocument = await _mergeService.MergeDefinitionsAsync();

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
				_cache.Set(_mergedEndpoint!, mergedDocument,
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
			_logger.LogError(ex, "Failed to generate Koalesced definition.");
			context.Response.StatusCode = StatusCodes.Status500InternalServerError;
			context.Response.ContentType = "application/json";

			// Return structured error response without exposing internal details
			var errorResponse = System.Text.Json.JsonSerializer.Serialize(new
			{
				error = "Failed to merge API definitions",
				message = "An error occurred while generating the merged OpenAPI specification. Check server logs for details.",
				traceId = context.TraceIdentifier
			});

			await context.Response.WriteAsync(errorResponse);
		}
	}
}