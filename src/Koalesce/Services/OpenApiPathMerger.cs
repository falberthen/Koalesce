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
		OpenApiServer? sourceServerEntry)
	{
		if (sourceDocument.Paths is null)
			return;

		var globalSecurityRequirements = sourceDocument.Security;

		foreach (var (originalPath, pathItem) in sourceDocument.Paths)
		{
			// Check Exclusions
			if (IsPathExcluded(originalPath, apiSource.ExcludePaths))
			{
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
		if ((operation.Security == null || !operation.Security.Any()) && globalSecurity?.Any() == true)
		{
			operation.Security = [.. globalSecurity];
		}

		operation.Summary ??= string.Empty;
	}

	/// <summary>
	/// Determines whether the specified path matches any of the provided exclusion patterns.
	/// Supports wildcards (*) anywhere in the pattern:
	/// - /api/* matches /api/users, /api/orders, etc.
	/// - /*/health matches /users/health, /orders/health, etc.
	/// - /api/*/details matches /api/users/details, /api/orders/details, etc.
	/// </summary>
	private static bool IsPathExcluded(string path, List<string>? excludePaths)
	{
		if (excludePaths == null || excludePaths.Count == 0)
			return false;

		foreach (var pattern in excludePaths)
		{
			if (string.IsNullOrWhiteSpace(pattern))
				continue;

			if (MatchesWildcardPattern(path, pattern))
				return true;
		}

		return false;
	}

	/// <summary>
	/// Matches a path against a wildcard pattern.
	/// * matches any single path segment (e.g., "users", "123", etc.)
	/// </summary>
	private static bool MatchesWildcardPattern(string path, string pattern)
	{
		string normalizedPath = path.Trim('/');
		string normalizedPattern = pattern.Trim('/');

		// Fast path: no wildcards, do exact match
		if (!normalizedPattern.Contains('*'))
		{
			return normalizedPath.Equals(normalizedPattern, StringComparison.OrdinalIgnoreCase);
		}

		var pathSegments = normalizedPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
		var patternSegments = normalizedPattern.Split('/', StringSplitOptions.RemoveEmptyEntries);

		// Special case: pattern ends with /* (match prefix and any sub-paths)
		if (patternSegments.Length > 0 && patternSegments[^1] == "*")
		{
			// Check if all segments before the trailing * match
			var prefixSegments = patternSegments[..^1];

			if (pathSegments.Length < prefixSegments.Length)
				return false;

			for (int i = 0; i < prefixSegments.Length; i++)
			{
				if (!SegmentMatches(pathSegments[i], prefixSegments[i]))
					return false;
			}

			return true;
		}

		// Standard case: segment count must match exactly
		if (pathSegments.Length != patternSegments.Length)
			return false;

		for (int i = 0; i < patternSegments.Length; i++)
		{
			if (!SegmentMatches(pathSegments[i], patternSegments[i]))
				return false;
		}

		return true;
	}

	/// <summary>
	/// Matches a single path segment against a pattern segment.
	/// Supports: *, prefix*, *suffix, pre*fix
	/// </summary>
	private static bool SegmentMatches(string segment, string pattern)
	{
		// Exact wildcard matches any segment
		if (pattern == "*")
			return true;

		// No wildcard, exact match required
		if (!pattern.Contains('*'))
			return segment.Equals(pattern, StringComparison.OrdinalIgnoreCase);

		// Wildcard in pattern: convert to simple matching
		int wildcardIndex = pattern.IndexOf('*');
		string prefix = pattern[..wildcardIndex];
		string suffix = pattern[(wildcardIndex + 1)..];

		bool prefixMatches = string.IsNullOrEmpty(prefix) ||
			segment.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);

		bool suffixMatches = string.IsNullOrEmpty(suffix) ||
			segment.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);

		// Ensure prefix and suffix don't overlap
		if (prefixMatches && suffixMatches)
		{
			return segment.Length >= prefix.Length + suffix.Length;
		}

		return false;
	}
}