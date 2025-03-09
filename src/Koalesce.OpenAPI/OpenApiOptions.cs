namespace Koalesce.OpenAPI;

/// <summary>
/// Options for configuring OpenAPI middleware.
/// </summary>
public class OpenApiOptions : KoalesceOptions
{
	public const string OpenApiVersionDefaultValue = "3.0.1";

	/// <summary>
	/// The OpenAPI specification version
	/// </summary>
	public string OpenApiVersion { get; set; } = OpenApiVersionDefaultValue;
}
