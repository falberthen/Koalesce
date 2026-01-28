namespace Koalesce.Core.Options;

/// <summary>
/// Represents a single API source to be merged.
/// Either Url or FilePath must be specified, but not both.
/// </summary>
public record class ApiSource
{
	/// <summary>
	/// The HTTP/HTTPS URL of the source API definition.
	/// Mutually exclusive with FilePath.
	/// </summary>
	public string? Url { get; set; }

	/// <summary>
	/// The local file path to the source API definition (JSON or YAML).
	/// Mutually exclusive with Url.
	/// </summary>
	public string? FilePath { get; set; }

	/// <summary>
	/// Optional virtual prefix to apply to routes and avoid collisions.
	/// </summary>
	public string? VirtualPrefix { get; set; } = default!;

	/// <summary>
	/// Optional list of paths to exclude from the merged document.
	/// Supports exact matches (e.g., "/api/internal") and wildcard patterns (e.g., "/api/admin/*").
	/// </summary>
	public List<string>? ExcludePaths { get; set; }
}