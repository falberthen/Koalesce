namespace Koalesce.OpenAPI;

/// <summary>
/// Extension methods for configuring Koalesce with OpenAPI.
/// </summary>
public static class OpenApiExtensions
{
	/// <summary>
	/// Registers the OpenAPI provider and required services for Koalesce.
	/// </summary>
	public static IKoalesceBuilder ForOpenAPI(this IKoalesceBuilder builder)
	{
		var services = builder.Services;
		services.TryAddSingleton(typeof(IOpenApiDocumentBuilder), typeof(OpenApiDocumentBuilder<OpenApiOptions>));
		return builder.AddProvider<OpenApiProvider, OpenApiOptions>();
	}
}
