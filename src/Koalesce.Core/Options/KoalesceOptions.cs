/// <summary>
/// Main configuration settings for Koalesce.
/// </summary>
public class KoalesceOptions : IValidatableObject
{
	public const string ConfigurationSectionName = "Koalesce";
	public const string TitleDefaultValue = "My Koalesced API";

	/// <summary>
	/// Source URLs. At least one source is required.
	/// </summary>
	[Required]	
	public List<ApiSource> Sources { get; set; } = new();

	/// <summary>
	/// The logical path where the merged definition should be exposed.
	/// </summary>	
	[Required]	
	public string MergedDocumentPath { get; set; } = default!;

	/// <summary>
	/// Koalesced API title
	/// </summary>
	public string Title { get; set; } = TitleDefaultValue;

	/// <summary>
	/// Caching configuration settings for Koalesce.
	/// </summary>
	public KoalesceCacheOptions Cache { get; set; } = new();

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
	/// Default: "{Prefix}_{SchemaName}"
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
		// Validating Sources
		if (Sources == null || !Sources.Any())
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

		// Validating MergedDocumentPath
		if (string.IsNullOrWhiteSpace(MergedDocumentPath))
		{
			yield return new ValidationResult(
				"MergedDocumentPath cannot be empty.",
				[nameof(MergedDocumentPath)]);
		}

		if (!MergedDocumentPath.StartsWith("/"))
		{
			yield return new ValidationResult(
				"MergedDocumentPath must start with '/'.",
				[nameof(MergedDocumentPath)]);
		}

		// Caching Validations
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

		// Validate SchemaConflictPattern contains required placeholders
		if (!string.IsNullOrWhiteSpace(SchemaConflictPattern))
		{
			bool hasPrefix = SchemaConflictPattern.Contains(CoreConstants.PrefixPlaceholder);
			bool hasSchemaName = SchemaConflictPattern.Contains(CoreConstants.SchemaNamePlaceholder);

			if (!hasPrefix || !hasSchemaName)
			{
				yield return new ValidationResult(
					CoreConstants.SchemaConflictPatternValidationError,
					[nameof(SchemaConflictPattern)]);
			}
		}

		// Validate HttpTimeoutSeconds
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
					[$"{nameof(Sources)}[{i}]"]);
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
						[$"{nameof(Sources)}[{i}].Url"]);
				}
			}

			// Validate FilePath exists
			if (hasFilePath)
			{
				// Resolve relative paths against the application base directory
				string resolvedPath = Path.IsPathRooted(source.FilePath!)
					? source.FilePath!
					: Path.Combine(AppContext.BaseDirectory, source.FilePath!);

				if (!File.Exists(resolvedPath))
				{
					yield return new ValidationResult(
						string.Format(CoreConstants.SourceFilePathNotFound, i, source.FilePath),
						[$"{nameof(Sources)}[{i}].FilePath"]);
				}
			}

			// Validate ExcludePaths
			foreach (var validationResult in ValidateExcludePaths(source, i))
				yield return validationResult;
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
					[$"{nameof(Sources)}[{sourceIndex}].ExcludePaths[{j}]"]);
				continue;
			}

			if (!path.StartsWith('/'))
			{
				yield return new ValidationResult(
					string.Format(CoreConstants.ExcludePathMustStartWithSlash, j, sourceIndex, path),
					[$"{nameof(Sources)}[{sourceIndex}].ExcludePaths[{j}]"]);
			}

			// Validate wildcard usage: only "/*" at the end is supported
			int wildcardIndex = path.IndexOf('*');
			if (wildcardIndex != -1)
			{
				bool isValidWildcard = path.EndsWith("/*") && path.IndexOf('*') == path.Length - 1;
				if (!isValidWildcard)
				{
					yield return new ValidationResult(
						string.Format(CoreConstants.ExcludePathInvalidWildcard, j, sourceIndex, path),
						[$"{nameof(Sources)}[{sourceIndex}].ExcludePaths[{j}]"]);
				}
			}
		}
	}
}