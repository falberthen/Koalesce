﻿namespace Koalesce.Core;

/// <summary>
/// Middleware to handle API requests.
/// </summary>
public class KoalesceMiddleware
{
	private readonly string _mergedOpenApiPath;
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
		_mergedOpenApiPath = opts.MergedOpenApiPath;
	}

	/// <summary>
	/// Middleware invocation logic.
	/// </summary>
	public async Task InvokeAsync(HttpContext context)
	{
		_logger.LogInformation("Request Path: {RequestPath}, Expected Koalesced Path: {MergePath}",
			context.Request.Path, _mergedOpenApiPath);

		// If the request path matches the expected merged merged path
		if (context.Request.Path.Equals(_mergedOpenApiPath, StringComparison.OrdinalIgnoreCase))
		{
			_logger.LogInformation($"Handling Koalesced API request: {_mergedOpenApiPath}.");

			// If cache is disabled, always recompute
			if (_cacheOptions.DisableCache)
			{
				_logger.LogInformation("Cache is disabled. Always recomputing Koalesced document.");
				await RecomputeAndRespond(context);
				return;
			}

			// Try fetching from cache first
			if (_cache.TryGetValue(_mergedOpenApiPath, out string cachedDocument))
			{
				_logger.LogInformation("Returning cached Koalesced document.");
				context.Response.ContentType = "application/json";
				await context.Response.WriteAsync(cachedDocument);
				return;
			}

			_logger.LogInformation("Cache expired or not found. Rebuilding Koalesced document...");
			await RecomputeAndRespond(context);
		}
		else
		{
			await _next(context);
		}
	}

	/// <summary>
	/// Recomputes OpenAPI document and stores it in cache (if enabled).
	/// </summary>
	private async Task RecomputeAndRespond(HttpContext context)
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
				_cache.Set(_mergedOpenApiPath, mergedDocument,
					new MemoryCacheEntryOptions()
						.SetAbsoluteExpiration(TimeSpan.FromSeconds(_cacheOptions.AbsoluteExpirationSeconds))
						.SetSlidingExpiration(TimeSpan.FromSeconds(_cacheOptions.SlidingExpirationSeconds))
				);
			}

			context.Response.ContentType = "application/json";
			await context.Response.WriteAsync(mergedDocument);
		}
		catch (Exception ex) when (ex is KoalesceIdenticalPathFoundException || ex is Exception)
		{
			context.Response.StatusCode = StatusCodes.Status500InternalServerError;
			await context.Response.WriteAsync(ex.Message);
		}
	}
}