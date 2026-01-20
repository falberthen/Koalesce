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
			foreach (var validationResult in ValidateSourceUrls()) 
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
	}

	private IEnumerable<ValidationResult> ValidateSourceUrls()
	{
		for (int i = 0; i < Sources.Count; i++)
		{
			var source = Sources[i];

			if (string.IsNullOrWhiteSpace(source.Url))
			{
				yield return new ValidationResult(
					$"The Source URL at index {i} cannot be empty.",
					[$"{nameof(Sources)}[{i}].Url"]);
				continue;
			}

			// Note: if Koalesce supports non-HTTP protocols in the future (e.g., amqp://, file://),
			// this validation will need to be relaxed or moved to the specific Provider.
			// I'm keeping HTTP/HTTPS for now, as the Core uses HttpClient for now.
			bool isUriValid = Uri.TryCreate(source.Url, UriKind.Absolute, out Uri? uriResult)
				&& (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

			if (!isUriValid)
			{
				yield return new ValidationResult(
					$"The Source URL at index {i} ('{source.Url}') must be a valid absolute URL (starting with http:// or https://).",
					[$"{nameof(Sources)}[{i}].Url"]);
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