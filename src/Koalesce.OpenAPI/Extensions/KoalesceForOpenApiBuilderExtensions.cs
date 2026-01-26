using Koalesce.OpenAPI.Services.ConflictResolution;

namespace Koalesce.OpenAPI.Extensions;

/// <summary>
/// Extension methods for configuring Koalesce with OpenAPI
/// </summary>
public static class KoalesceForOpenApiBuilderExtensions
{
	/// <summary>
	/// Registers the OpenAPI provider and required services for Koalesce
	/// </summary>
	/// <param name="builder">The Koalesce builder.</param>
	/// <param name="configureOptions">Optional delegate to configure OpenApiOptions</param>
	public static IKoalesceBuilder ForOpenAPI(
		this IKoalesceBuilder builder,
		Action<KoalesceOpenApiOptions>? configureOptions = null)
	{
		var services = builder.Services;

		// Implementations of Koalesce basic services
		services.TryAddSingleton(typeof(IDocumentMerger<OpenApiDocument>), typeof(OpenApiDocumentMerger));
		services.TryAddSingleton(typeof(IMergedDocumentSerializer<OpenApiDocument>), typeof(OpenApiDocumentSerializer));

		// Supporting Services for merging
		services.TryAddSingleton<OpenApiDefinitionLoader>();
		services.TryAddSingleton<OpenApiPathMerger>();

		// Conflict Resolution Services
		services.TryAddSingleton<IConflictResolutionStrategy, DefaultConflictResolutionStrategy>();
		services.TryAddSingleton<SchemaRenamer>();
		services.TryAddSingleton<SchemaConflictCoordinator>();

		// Registering provider
		services.TryAddSingleton<KoalesceOpenApiProvider>();
		services.TryAddSingleton<IKoalesceProvider, KoalesceOpenApiProvider>();

		// Apply Code-based Configuration (Extension Methods)
		// PostConfigure runs AFTER the JSON binding invoked by AddProvider
		if (configureOptions != null)
		{
			services.PostConfigure(configureOptions);
		}

		// Registers the provider and binds the base JSON section
		return builder.AddProvider<KoalesceOpenApiProvider, KoalesceOpenApiOptions>();
	}
}