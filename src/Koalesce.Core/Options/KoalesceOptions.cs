/// <summary>
/// Main configuration settings for Koalesce.
/// </summary>
public class KoalesceOptions : IValidatableObject
{
	public const string ConfigurationSectionName = "Koalesce";
	public const string TitleDefaultValue = "My Koalesced API";

	/// <summary>
	/// Source OpenAPI URLs. At least one source is required.
	/// </summary>
	[Required]
	public List<string> SourceOpenApiUrls { get; set; }

	/// <summary>
	/// The logical path where the merged API definition should be exposed.
	/// </summary>
	[Required]
	public string MergedOpenApiPath { get; set; }

	/// <summary>
	/// Koalesced API title
	/// </summary>
	public string Title { get; set; } = TitleDefaultValue;

	/// <summary>
	/// Caching configuration settings for Koalesce.
	/// </summary>
	public KoalesceCacheOptions Cache { get; set; } = new();

	/// <summary>
	/// BaseUrl if using an API Gateway
	/// </summary>
	public string ApiGatewayBaseUrl { get; set; }

	/// <summary>
	/// Determines whether Koalesce skips identical paths.
	/// When set to true (default), identical paths are ignored/skipped.
	/// When set to false, Koalesce throws an exception when detecting identical
	///  paths while merging APIs.
	/// </summary>
	public bool SkipIdenticalPaths { get; set; } = true;

	/// <summary>
	/// Custom validation logic for required fields.
	/// </summary>	
	public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
	{
		// Validating SourceOpenApiUrls
		if (SourceOpenApiUrls == null || !SourceOpenApiUrls.Any())
		{
			yield return new ValidationResult(
				"At least one source API URL must be defined in SourceOpenApiUrls.",
				new[]{ nameof(SourceOpenApiUrls) });
		}

		// Validating MergedOpenApiPath
		if (string.IsNullOrWhiteSpace(MergedOpenApiPath))
		{
			yield return new ValidationResult(
				"MergedOpenApiPath cannot be empty.",
				new[] { nameof(MergedOpenApiPath) });
		}

		if (!MergedOpenApiPath.StartsWith("/"))
		{
			yield return new ValidationResult(
				"MergedOpenApiPath must start with '/'.",
				new[] { nameof(MergedOpenApiPath) });
		}

		// Caching Validations
		if (Cache.MinExpirationSeconds < 0)
		{
			yield return new ValidationResult(
				"MinExpirationSeconds must be a positive value.",
				new[] { nameof(Cache.MinExpirationSeconds) });
		}

		if (Cache.AbsoluteExpirationSeconds < Cache.MinExpirationSeconds)
		{
			yield return new ValidationResult(
				$"AbsoluteExpirationSeconds ({Cache.AbsoluteExpirationSeconds}) must be at least MinExpirationSeconds ({Cache.MinExpirationSeconds}).",
				new[] { nameof(Cache.AbsoluteExpirationSeconds) });
		}

		if (Cache.SlidingExpirationSeconds < Cache.MinExpirationSeconds)
		{
			yield return new ValidationResult(
				$"SlidingExpirationSeconds ({Cache.SlidingExpirationSeconds}) must be at least MinExpirationSeconds ({Cache.MinExpirationSeconds}).",
				new[] { nameof(Cache.SlidingExpirationSeconds) });
		}

		if (Cache.SlidingExpirationSeconds > Cache.AbsoluteExpirationSeconds)
		{
			yield return new ValidationResult(
				$"SlidingExpirationSeconds ({Cache.SlidingExpirationSeconds}) cannot be greater than AbsoluteExpirationSeconds ({Cache.AbsoluteExpirationSeconds}).",
				new[] { nameof(Cache.SlidingExpirationSeconds) });
		}
	}
}