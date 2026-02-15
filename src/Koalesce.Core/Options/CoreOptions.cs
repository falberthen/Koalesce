namespace Koalesce.Core.Options;

/// <summary>
/// Main configuration settings for Koalesce.
/// </summary>
public class CoreOptions : IValidatableObject
{
	public const string ConfigurationSectionName = "Koalesce";

	/// <summary>
	/// Source URLs. At least one source is required.
	/// </summary>
	[Required]
	public List<ApiSource> Sources { get; set; } = [];

	/// <summary>
	/// The endpoint path where the merged definition should be exposed.
	/// Required only when using Koalesce middleware.
	/// </summary>
	public string? MergedEndpoint { get; set; } = default!;

	/// <summary>
	/// Optional endpoint path where the merge report is exposed as JSON.
	/// When configured, the middleware serves a structured report at this path.
	/// </summary>
	public string? MergeReportEndpoint { get; set; }

	/// <summary>
	/// Caching configuration settings for Koalesce.
	/// </summary>
	public CacheOptions Cache { get; set; } = new();

	/// <summary>
	/// Determines whether Koalesce skips identical paths.
	/// When set to true (default), identical paths are ignored/skipped.
	/// When set to false, Koalesce throws an exception when detecting identical
	///  paths while merging APIs.
	/// </summary>
	public bool SkipIdenticalPaths { get; set; } = true;

	/// <summary>
	/// Pattern for resolving schema name conflicts.
	/// Available placeholders: {Prefix}, {SchemaName}
	/// Default: "{Prefix}{SchemaName}"
	/// </summary>
	public string SchemaConflictPattern { get; set; } = CoreConstants.DefaultSchemaConflictPattern;

	/// <summary>
	/// If true, the merge process will fail and throw an exception if ANY source API cannot be loaded.
	/// If false (default), unreachable sources are logged and skipped, allowing the Gateway to start partially.
	/// </summary>
	public bool FailOnServiceLoadError { get; set; } = false;

	/// <summary>
	/// HTTP request timeout in seconds for fetching API specifications.
	/// Default: 15 seconds.
	/// </summary>
	public int HttpTimeoutSeconds { get; set; } = CoreConstants.DefaultHttpTimeoutSeconds;

	/// <summary>
	/// Custom validation logic for required fields.
	/// </summary>
	public virtual IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
	{
		foreach (var result in ValidateSourcesRequired())
			yield return result;

		foreach (var result in ValidateMergedEndpoint())
			yield return result;

		foreach (var result in ValidateMergeReportEndpoint())
			yield return result;

		foreach (var result in ValidateCacheSettings())
			yield return result;

		foreach (var result in ValidateSchemaConflictPattern())
			yield return result;

		foreach (var result in ValidateHttpTimeout())
			yield return result;
	}

	private IEnumerable<ValidationResult> ValidateSourcesRequired()
	{
		if (Sources == null || Sources.Count == 0)
		{
			yield return new ValidationResult(
				"At least one source must be defined in Sources.",
				[nameof(Sources)]);
		}
		else
		{
			foreach (var validationResult in ValidateSources())
				yield return validationResult;
		}
	}

	private IEnumerable<ValidationResult> ValidateMergedEndpoint()
	{
		if (!string.IsNullOrWhiteSpace(MergedEndpoint) && !MergedEndpoint.StartsWith("/"))
		{
			yield return new ValidationResult(
				"MergedEndpoint must start with '/'.",
				[nameof(MergedEndpoint)]);
		}
	}

	private IEnumerable<ValidationResult> ValidateMergeReportEndpoint()
	{
		if (string.IsNullOrWhiteSpace(MergeReportEndpoint))
			yield break;

		if (!MergeReportEndpoint.StartsWith("/"))
		{
			yield return new ValidationResult(
				"MergeReportEndpoint must start with '/'.",
				[nameof(MergeReportEndpoint)]);
		}

		if (!string.IsNullOrWhiteSpace(MergedEndpoint) &&
			string.Equals(MergeReportEndpoint, MergedEndpoint, StringComparison.OrdinalIgnoreCase))
		{
			yield return new ValidationResult(
				"MergeReportEndpoint must be different from MergedEndpoint.",
				[nameof(MergeReportEndpoint)]);
		}
	}

	private IEnumerable<ValidationResult> ValidateCacheSettings()
	{
		if (Cache.MinExpirationSeconds < 0)
		{
			yield return new ValidationResult(
				"MinExpirationSeconds must be a positive value.",
				[nameof(Cache.MinExpirationSeconds)]);
		}

		if (Cache.AbsoluteExpirationSeconds < Cache.MinExpirationSeconds)
		{
			yield return new ValidationResult(
				$"AbsoluteExpirationSeconds ({Cache.AbsoluteExpirationSeconds}) must be at least MinExpirationSeconds ({Cache.MinExpirationSeconds}).",
				[nameof(Cache.AbsoluteExpirationSeconds)]);
		}

		if (Cache.SlidingExpirationSeconds < Cache.MinExpirationSeconds)
		{
			yield return new ValidationResult(
				$"SlidingExpirationSeconds ({Cache.SlidingExpirationSeconds}) must be at least MinExpirationSeconds ({Cache.MinExpirationSeconds}).",
				[nameof(Cache.SlidingExpirationSeconds)]);
		}

		if (Cache.SlidingExpirationSeconds > Cache.AbsoluteExpirationSeconds)
		{
			yield return new ValidationResult(
				$"SlidingExpirationSeconds ({Cache.SlidingExpirationSeconds}) cannot be greater than AbsoluteExpirationSeconds ({Cache.AbsoluteExpirationSeconds}).",
				[nameof(Cache.SlidingExpirationSeconds)]);
		}
	}

	private IEnumerable<ValidationResult> ValidateSchemaConflictPattern()
	{
		if (string.IsNullOrWhiteSpace(SchemaConflictPattern))
			yield break;

		bool hasPrefix = SchemaConflictPattern.Contains(CoreConstants.PrefixPlaceholder);
		bool hasSchemaName = SchemaConflictPattern.Contains(CoreConstants.SchemaNamePlaceholder);

		if (!hasPrefix || !hasSchemaName)
		{
			yield return new ValidationResult(
				CoreConstants.SchemaConflictPatternValidationError,
				[nameof(SchemaConflictPattern)]);
		}
	}

	private IEnumerable<ValidationResult> ValidateHttpTimeout()
	{
		if (HttpTimeoutSeconds <= 0)
		{
			yield return new ValidationResult(
				CoreConstants.HttpTimeoutMustBePositive,
				[nameof(HttpTimeoutSeconds)]);
		}
	}

	private IEnumerable<ValidationResult> ValidateSources()
	{
		// Check for duplicate sources (same Url or FilePath)
		var sourceGroups = Sources
			.Select((source, index) => (source, index))
			.GroupBy(x => (x.source.Url ?? x.source.FilePath)?.ToLowerInvariant())
			.Where(g => g.Key is not null && g.Count() > 1);

		foreach (var group in sourceGroups)
		{
			var indices = string.Join(", ", group.Select(x => x.index));
			yield return new ValidationResult(
				string.Format(CoreConstants.DuplicateSourceFound, group.Key, indices),
				[nameof(Sources)]);
		}

		// Check for duplicate VirtualPrefix values
		var virtualPrefixGroups = Sources
			.Select((source, index) => (source, index))
			.Where(x => !string.IsNullOrWhiteSpace(x.source.VirtualPrefix))
			.GroupBy(x => x.source.VirtualPrefix!.Trim('/').ToLowerInvariant())
			.Where(g => g.Count() > 1);

		foreach (var group in virtualPrefixGroups)
		{
			var indices = string.Join(", ", group.Select(x => x.index));
			yield return new ValidationResult(
				$"Duplicate VirtualPrefix '/{group.Key}' found in Sources at indices [{indices}]. Each source must have a unique VirtualPrefix.",
				[nameof(Sources)]);
		}

		for (int i = 0; i < Sources.Count; i++)
		{
			var source = Sources[i];
			bool hasUrl = !string.IsNullOrWhiteSpace(source.Url);
			bool hasFilePath = !string.IsNullOrWhiteSpace(source.FilePath);

			// Validate Url XOR FilePath
			if (hasUrl == hasFilePath)
			{
				yield return new ValidationResult(
					string.Format(CoreConstants.SourceMustHaveUrlOrFilePath, i),
					[ValidationPath.Source(i)]);
				continue;
			}

			// Validate URL format
			if (hasUrl)
			{
				bool isUriValid = Uri.TryCreate(source.Url, UriKind.Absolute, out Uri? uriResult)
					&& (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

				if (!isUriValid)
				{
					yield return new ValidationResult(
						$"The Source URL at index {i} ('{source.Url}') must be a valid absolute URL (starting with http:// or https://).",
						[ValidationPath.SourceUrl(i)]);
				}
			}

			// Note: FilePath existence is validated at load time, not at startup.
			// This allows FailOnServiceLoadError to control the behavior.

			// Validate ExcludePaths
			foreach (var validationResult in ValidateExcludePaths(source, i))
				yield return validationResult;

			// Validate PrefixTagsWith
			if (source.PrefixTagsWith is not null && string.IsNullOrWhiteSpace(source.PrefixTagsWith))
			{
				yield return new ValidationResult(
					$"PrefixTagsWith at source index {i} cannot be empty or whitespace. Use null to disable tag prefixing.",
					[ValidationPath.Source(i)]);
			}
		}
	}

	private static IEnumerable<ValidationResult> ValidateExcludePaths(ApiSource source, int sourceIndex)
	{
		if (source.ExcludePaths == null || source.ExcludePaths.Count == 0)
			yield break;

		for (int j = 0; j < source.ExcludePaths.Count; j++)
		{
			var path = source.ExcludePaths[j];

			if (string.IsNullOrWhiteSpace(path))
			{
				yield return new ValidationResult(
					string.Format(CoreConstants.ExcludePathCannotBeEmpty, j, sourceIndex),
					[ValidationPath.ExcludePath(sourceIndex, j)]);
				continue;
			}

			// Path must start with '/' or '*' (for leading wildcard patterns like */admin/*)
			if (!path.StartsWith('/') && !path.StartsWith('*'))
			{
				yield return new ValidationResult(
					string.Format(CoreConstants.ExcludePathMustStartWithSlashOrWildcard, j, sourceIndex, path),
					[ValidationPath.ExcludePath(sourceIndex, j)]);
			}

			// Validate wildcard usage: * can appear anywhere but must match a single segment
			if (!IsValidWildcardPattern(path))
			{
				yield return new ValidationResult(
					string.Format(CoreConstants.ExcludePathInvalidWildcard, j, sourceIndex, path),
					[ValidationPath.ExcludePath(sourceIndex, j)]);
			}
		}
	}

	/// <summary>
	/// Validates that a wildcard pattern is well-formed.
	/// Wildcards (*) can appear anywhere but cannot be adjacent to other wildcards.
	/// </summary>
	private static bool IsValidWildcardPattern(string pattern)
	{
		int wildcardIndex = pattern.IndexOf('*');
		if (wildcardIndex == -1)
			return true;

		// Check for consecutive wildcards (e.g., "/**/foo" or "/foo**")
		if (pattern.Contains("**"))
			return false;

		// Check that wildcards are properly positioned (at segment boundaries or as full segments)
		var segments = pattern.Split('/');
		foreach (var segment in segments)
		{
			if (string.IsNullOrEmpty(segment))
				continue;

			// A segment can be:
			// - A literal (no wildcards): "users"
			// - A single wildcard: "*"
			// - A prefix wildcard: "*suffix"
			// - A suffix wildcard: "prefix*"
			// - A middle wildcard: "pre*fix"
			// All are valid as long as there's only one * per segment
			int wildcardCount = segment.Count(c => c == '*');
			if (wildcardCount > 1)
				return false;
		}

		return true;
	}
}