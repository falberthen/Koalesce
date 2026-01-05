namespace Koalesce.Core.Options;

/// <summary>
/// Represents a single API source to be merged.
/// </summary>
public record class OpenApiSourceDefinition
{
	/// <summary>
	/// The source URL of the OpenAPI definition (JSON).
	/// </summary>
	public required string Url { get; set; }

	/// <summary>
	/// Optional virtual prefix to apply to routes and avoid collisions.	
	/// </summary>
	public string? VirtualPrefix { get; set; } = default!;
}
