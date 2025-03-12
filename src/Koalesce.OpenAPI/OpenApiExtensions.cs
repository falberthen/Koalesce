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

		// Implementations of Koalesce basic services
		services.TryAddSingleton(typeof(IDocumentMerger<OpenApiDocument>), typeof(OpenApiDocumentMerger));
		services.TryAddSingleton(typeof(IMergedDocumentSerializer<OpenApiDocument>), typeof(OpenApiDocumentSerializer));

		// Registering provider
		services.TryAddSingleton<OpenApiProvider>();
		services.TryAddSingleton<IKoalesceProvider, OpenApiProvider>();
		return builder.AddProvider<OpenApiProvider, OpenApiOptions>();
	}
}
