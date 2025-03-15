/// <summary>
/// Basic options for configuring Koalesce middleware.
/// </summary>
public class KoalesceOptions : IValidatableObject
{
	public const string ConfigurationSectionName = "Koalesce";
	public const string TitleDefaultValue = "My 🐨Koalesced API";

	/// <summary>
	/// Source OpenAPI urls. At least one source is required.
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
	/// Cache duration for merged documents
	/// </summary>
	public int CacheDurationSeconds
	{
		get => _cacheDurationSeconds;
		set => _cacheDurationSeconds = (value > 0 && value <= _maxDurationSeconds)
			? value
			: throw new ArgumentOutOfRangeException(nameof(CacheDurationSeconds),
				$"Cache duration must be between 1 and {_maxDurationSeconds} seconds."); 
	}

	/// <summary>
	/// Flag for disabling middleware caching
	/// </summary>
	public bool DisableCache { get; set; } = false; // Default: Caching enabled

	/// <summary>
	/// BaseUrl if using an API Gateway
	/// </summary>
	public string ApiGatewayBaseUrl { get; set; }

	/// <summary>
	/// Determines whether Koalesce skips identical paths.
	/// When set to true (default), identical paths are ignored/skipped.
	/// When set to false, Koalesce throws an exception at build time.
	/// </summary>
	public bool SkipIdenticalPaths { get; set; } = true;

	/// <summary>
	/// Custom validation logic for required fields.
	/// </summary>
	public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
	{
		// Check SourceOpenApiUrls
		if (SourceOpenApiUrls == null || !SourceOpenApiUrls.Any())
		{
			yield return new ValidationResult("At least one source API url must be defined in SourceOpenApiUrls.", 
				new[] { nameof(SourceOpenApiUrls) });
		}

		// MergedOpenApiPath
		if (string.IsNullOrWhiteSpace(MergedOpenApiPath))
			yield return new ValidationResult("MergedOpenApiPath cannot be empty.", new[] { nameof(MergedOpenApiPath) });
		if (!MergedOpenApiPath.StartsWith("/"))
			yield return new ValidationResult("MergedOpenApiPath must start with '/'.", new[] { nameof(MergedOpenApiPath) });

		// Caching
		if (CacheDurationSeconds < 0)
			yield return new ValidationResult("CacheDurationSeconds must be non-negative.");
	}

	/// <summary>
	/// Cache duration for the merged OpenAPI document.
	/// Default: 5 minutes (300 seconds).
	/// </summary>
	private int _cacheDurationSeconds = 300; // Default 5 min
	private int _maxDurationSeconds = 3600; // TODO: should it be configurable?
}