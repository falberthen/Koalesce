using Koalesce.Services.Report;

namespace Koalesce.Services;

/// <summary>
/// Service responsible for merging OpenAPI paths from source documents into a target document.
/// </summary>
internal class OpenApiPathMerger
{
	private readonly KoalesceOptions _options;
	private readonly ILogger<OpenApiPathMerger> _logger;

	public OpenApiPathMerger(
		IOptions<KoalesceOptions> options,
		ILogger<OpenApiPathMerger> logger)
	{
		_options = options.Value;
		_logger = logger;
	}

	/// <summary>
	/// Merges the paths from the specified OpenAPI source document into the target paths collection, applying exclusions,
	/// virtual prefixing, and collision handling as configured.
	/// </summary>	
	public void MergePaths(
		OpenApiDocument sourceDocument,
		OpenApiPaths targetPaths,
		string apiName,
		ApiSource apiSource,
		OpenApiServer? sourceServerEntry,
		MergeReportBuilder reportBuilder)
	{
		if (sourceDocument.Paths is null)
			return;

		var globalSecurityRequirements = sourceDocument.Security;

		foreach (var (originalPath, pathItem) in sourceDocument.Paths)
		{
			// Check Exclusions
			var matchedPattern = GetMatchedExclusionPattern(originalPath, apiSource.ExcludePaths);
			if (matchedPattern is not null)
			{
				reportBuilder.AddExcludedPath(originalPath, apiName, matchedPattern);
				_logger.LogInformation("Excluding path '{Path}' from '{ApiName}'", originalPath, apiName);
				continue;
			}

			// Generate New Path Key (Prefixing)
			string newPathKey = string.IsNullOrEmpty(apiSource.VirtualPrefix)
				? originalPath
				: $"/{apiSource.VirtualPrefix.Trim('/')}/{originalPath.TrimStart('/')}";

			// Collision Check
			if (targetPaths.ContainsKey(newPathKey))
			{
				if (!_options.SkipIdenticalPaths)
				{
					throw new KoalesceIdenticalPathFoundException(newPathKey, apiName);
				}

				reportBuilder.AddSkippedPath(newPathKey, apiName);
				_logger.LogWarning("Skipping identical path '{Path}' from '{ApiName}'.", newPathKey, apiName);

				continue;
			}

			// Create New Path Item
			var newPathItem = new OpenApiPathItem
			{
				Summary = pathItem.Summary,
				Description = pathItem.Description,
				Parameters = pathItem.Parameters?.ToList() ?? []
			};

			targetPaths[newPathKey] = newPathItem;
			reportBuilder.IncrementPathsMerged();

			// Process Operations
			if (pathItem.Operations is null) continue;

			foreach (var (opType, operation) in pathItem.Operations)
			{
				ProcessOperation(operation, apiSource, sourceServerEntry, globalSecurityRequirements);
				newPathItem.Operations ??= new Dictionary<HttpMethod, OpenApiOperation>();
				newPathItem.Operations[opType] = operation;
			}
		}
	}

	/// <summary>
	/// Processes and updates the specified OpenAPI operation with namespacing, server information, and summary defaults
	/// based on the provided API source and server entry.
	/// </summary>	
	private void ProcessOperation(
		OpenApiOperation operation,
		ApiSource apiSource,
		OpenApiServer? serverEntry,
		IList<OpenApiSecurityRequirement>? globalSecurity)
	{
		// Namespace OperationId
		if (!string.IsNullOrEmpty(operation.OperationId) && !string.IsNullOrEmpty(apiSource.VirtualPrefix))
		{
			string cleanPrefix = apiSource.VirtualPrefix.Replace("/", "").Replace("-", "");
			operation.OperationId = $"{cleanPrefix}_{operation.OperationId}";
		}

		// Handle Servers
		if (!string.IsNullOrEmpty(_options.ApiGatewayBaseUrl))
		{
			operation.Servers?.Clear();
		}
		else if (serverEntry != null)
		{
			operation.Servers ??= [];
			if (!operation.Servers.Any(s => s.Url == serverEntry.Url))
				operation.Servers.Add(serverEntry);
		}

		// Materialize Security
		if ((operation.Security == null || operation.Security.Count == 0) 
			&& globalSecurity?.Count > 0)
		{
			operation.Security = [.. globalSecurity];
		}

		operation.Summary ??= string.Empty;
	}

	/// <summary>
	/// Returns the first exclusion pattern that matches the given path, or null if no pattern matches.
	/// Supports wildcards (*) anywhere in the pattern:
	/// - /api/* matches /api/users, /api/orders, etc.
	/// - /*/health matches /users/health, /orders/health, etc.
	/// - /api/*/details matches /api/users/details, /api/orders/details, etc.
	/// </summary>
	private static string? GetMatchedExclusionPattern(string path, List<string>? excludePaths)
	{
		if (excludePaths == null || excludePaths.Count == 0)
			return null;

		foreach (var pattern in excludePaths)
		{
			if (string.IsNullOrWhiteSpace(pattern))
				continue;

			if (MatchesWildcardPattern(path, pattern))
				return pattern;
		}

		return null;
	}

	private static readonly ConcurrentDictionary<string, Regex> _wildcardRegexCache = new();

	/// <summary>
	/// Matches a path against a wildcard pattern using regex.
	/// - /admin/* matches /admin/users, /admin/products, etc.
	/// - */admin/* matches /api/admin/users, /v1/admin/products, etc.
	/// - /api/*/details matches /api/users/details, /api/orders/details, etc.
	/// </summary>
	private static bool MatchesWildcardPattern(string path, string pattern)
	{
		string normalizedPath = path.Trim('/');
		string normalizedPattern = pattern.Trim('/');

		// Fast path: no wildcards, do exact match
		if (!normalizedPattern.Contains('*'))
			return normalizedPath.Equals(normalizedPattern, StringComparison.OrdinalIgnoreCase);

		var regex = _wildcardRegexCache.GetOrAdd(normalizedPattern, static p =>
		{
			// Convert pattern to regex:
			// - Leading * means "anything before" (.*/)?
			// - Trailing * means "anything after" (/.*)?
			// - Middle * means "one segment" [^/]+
			string regexPattern = Regex.Escape(p);

			// Handle leading wildcard: */admin -> (.*\/)?admin		
			if (regexPattern.StartsWith(@"\*/"))
				regexPattern = @"(.*/)?" + regexPattern[3..];
			else if (regexPattern.StartsWith(@"\*"))
				regexPattern = @"(.*/)?" + regexPattern[2..];

			// Handle trailing wildcard: admin/* -> admin(/.*)?
			if (regexPattern.EndsWith(@"/\*"))
				regexPattern = regexPattern[..^3] + @"(/.*)?";
			else if (regexPattern.EndsWith(@"\*"))
				regexPattern = regexPattern[..^2] + @"(/.*)?";

			// Handle middle wildcards: /api/*/details -> /api/[^/]+/details
			regexPattern = regexPattern.Replace(@"\*", @"[^/]+");

			return new Regex($"^{regexPattern}$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
		});

		return regex.IsMatch(normalizedPath);
	}
}