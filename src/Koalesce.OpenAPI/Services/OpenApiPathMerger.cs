namespace Koalesce.OpenAPI.Services;

internal class OpenApiPathMerger
{
	private readonly KoalesceOpenApiOptions _options;
	private readonly ILogger<OpenApiPathMerger> _logger;

	public OpenApiPathMerger(
		IOptions<KoalesceOpenApiOptions> options,
		ILogger<OpenApiPathMerger> logger)
	{
		_options = options.Value;
		_logger = logger;
	}

	public void MergePaths(
		OpenApiDocument sourceDocument,
		OpenApiPaths targetPaths,
		string apiName,
		ApiSource apiSource,
		OpenApiServer? sourceServerEntry)
	{
		var globalSecurityRequirements = sourceDocument.SecurityRequirements;

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
				Parameters = pathItem.Parameters?.Select(p => p).ToList() ?? new List<OpenApiParameter>()
			};

			targetPaths[newPathKey] = newPathItem;

			// Process Operations
			foreach (var (opType, operation) in pathItem.Operations)
			{
				ProcessOperation(operation, apiSource, sourceServerEntry, globalSecurityRequirements);
				newPathItem.Operations[opType] = operation;
			}
		}
	}

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
			operation.Servers ??= new List<OpenApiServer>();
			if (!operation.Servers.Any(s => s.Url == serverEntry.Url))
			{
				operation.Servers.Add(serverEntry);
			}
		}

		// Materialize Security
		if ((operation.Security == null || !operation.Security.Any()) && globalSecurity?.Any() == true)
		{
			operation.Security = [.. globalSecurity];
		}

		operation.Summary ??= string.Empty;
	}

	private static bool IsPathExcluded(string path, List<string>? excludePaths)
	{
		if (excludePaths == null || excludePaths.Count == 0) return false;

		foreach (var pattern in excludePaths)
		{
			if (string.IsNullOrWhiteSpace(pattern)) continue;

			string normalizedPath = path.TrimEnd('/');
			string normalizedPattern = pattern.TrimEnd('/');

			if (normalizedPattern.EndsWith("/*"))
			{
				string prefix = normalizedPattern[..^2];
				if (normalizedPath.Equals(prefix, StringComparison.OrdinalIgnoreCase) ||
					normalizedPath.StartsWith(prefix + "/", StringComparison.OrdinalIgnoreCase))
					return true;
			}
			else if (normalizedPath.Equals(normalizedPattern, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}

		return false;
	}
}