namespace Koalesce.Core;

/// <summary>
/// Middleware to handle API requests
/// </summary>
public class KoalesceMiddleware
{
	private readonly string? _mergedEndpoint;
	private readonly string? _mergeReportEndpoint;
	private readonly bool _reportAsHtml;
	private readonly IKoalesceMergeService _mergeService;
	private readonly ILogger<KoalesceMiddleware> _logger;
	private readonly RequestDelegate _next;

	// Caching (for the merged document only)
	private readonly IMemoryCache _cache;
	private readonly CacheOptions _cacheOptions;

	// Report is stored as a simple field — always available after any merge,
	// independent of cache settings, and never expires on its own.
	private volatile MergeReport? _lastReport;

	private static readonly System.Text.Json.JsonSerializerOptions _reportJsonOptions = new()
	{
		PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
		DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
		WriteIndented = true
	};

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
		_mergeReportEndpoint = opts.MergeReportEndpoint;
		_reportAsHtml = _mergeReportEndpoint?.EndsWith(".html", StringComparison.OrdinalIgnoreCase) == true;
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

		bool isMergedRequest = context.Request.Path.Equals(_mergedEndpoint, StringComparison.OrdinalIgnoreCase);
		bool isReportRequest = _mergeReportEndpoint is not null
			&& context.Request.Path.Equals(_mergeReportEndpoint, StringComparison.OrdinalIgnoreCase);

		if (!isMergedRequest && !isReportRequest)
		{
			await _next(context);
			return;
		}

		// Report endpoint is read-only: it never triggers a merge.
		// It serves the last report produced by a merge, or empty content if no merge has occurred yet.
		if (isReportRequest)
		{
			await ServeReportAsync(context);
			return;
		}

		// Merged endpoint: trigger merge (with caching)
		if (_cacheOptions.DisableCache)
		{
			_logger.LogInformation("Cache is disabled. Always recomputing Koalesced document.");
			await RecomputeAndRespondAsync(context);
			return;
		}

		if (_cache.TryGetValue(_mergedEndpoint!, out string? cached) && !string.IsNullOrEmpty(cached))
		{
			_logger.LogInformation("Returning cached Koalesced document.");
			context.Response.ContentType = "application/json";
			await context.Response.WriteAsync(cached);
			return;
		}

		_logger.LogInformation("Cache expired or not found. Rebuilding Koalesced document...");
		await RecomputeAndRespondAsync(context);
	}

	/// <summary>
	/// Serves the merge report. The output format (JSON or HTML)
	/// is determined by the configured endpoint extension.
	/// Returns empty content if no merge has occurred yet.
	/// </summary>
	private async Task ServeReportAsync(HttpContext context)
	{
		var report = _lastReport;

		if (report is not null)
		{
			_logger.LogInformation("Returning merge report.");

			if (_reportAsHtml)
			{
				context.Response.ContentType = "text/html; charset=utf-8";
				await context.Response.WriteAsync(MergeReportHtmlRenderer.Render(report));
			}
			else
			{
				context.Response.ContentType = "application/json";
				await context.Response.WriteAsync(
					System.Text.Json.JsonSerializer.Serialize(report, _reportJsonOptions));
			}
			return;
		}

		_logger.LogInformation("No merge report available yet. Returning empty report.");

		if (_reportAsHtml)
		{
			context.Response.ContentType = "text/html; charset=utf-8";
			await context.Response.WriteAsync(
				"<html><body><p>No merge has been performed yet.</p></body></html>");
		}
		else
		{
			context.Response.ContentType = "application/json";
			await context.Response.WriteAsync("{}");
		}
	}

	/// <summary>
	/// Recomputes the merged OpenAPI document and stores it in cache.
	/// The report is always stored as a field, independent of cache settings.
	/// </summary>
	private async Task RecomputeAndRespondAsync(HttpContext context)
	{
		try
		{
			var result = await _mergeService.MergeSpecificationsAsync();
			string mergedDocument = result.SerializedDocument;

			if (string.IsNullOrWhiteSpace(mergedDocument))
			{
				context.Response.StatusCode = StatusCodes.Status404NotFound;
				await context.Response.WriteAsync("No APIs available for Koalescing.");
				return;
			}

			// Always store the report (independent of cache settings)
			if (result.Report is not null)
				_lastReport = result.Report;

			// Store merged document in cache if caching is enabled
			if (!_cacheOptions.DisableCache)
			{
				var cacheEntryOptions = BuildCacheEntryOptions();
				_cache.Set(_mergedEndpoint!, mergedDocument, cacheEntryOptions);
			}

			context.Response.ContentType = "application/json";
			await context.Response.WriteAsync(mergedDocument);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to generate Koalesced specification.");
			context.Response.StatusCode = StatusCodes.Status500InternalServerError;
			context.Response.ContentType = "application/json";

			var errorResponse = System.Text.Json.JsonSerializer.Serialize(new
			{
				error = "Failed to Koalesce API specifications.",
				message = "An error occurred while generating the merged OpenAPI specification. Check server logs for details.",
				traceId = context.TraceIdentifier
			});

			await context.Response.WriteAsync(errorResponse);
		}
	}

	private MemoryCacheEntryOptions BuildCacheEntryOptions()
	{
		var safeAbsoluteExpiration = Math.Max(
			_cacheOptions.AbsoluteExpirationSeconds,
			_cacheOptions.MinExpirationSeconds);

		var safeSlidingExpiration = Math.Max(
			_cacheOptions.SlidingExpirationSeconds,
			_cacheOptions.MinExpirationSeconds);

		return new MemoryCacheEntryOptions()
			.SetAbsoluteExpiration(TimeSpan.FromSeconds(safeAbsoluteExpiration))
			.SetSlidingExpiration(TimeSpan.FromSeconds(safeSlidingExpiration));
	}
}
