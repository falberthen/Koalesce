using Koalesce.OpenAPI.Options;

namespace Koalesce.OpenAPI.Providers;

/// <summary>
/// OpenAPI provider for Koalesce.
/// </summary>
public class KoalesceOpenApiProvider : KoalesceProviderBase<KoalesceOpenApiOptions, OpenApiDocument>
{
	public KoalesceOpenApiProvider(
		ILogger<KoalesceOpenApiProvider> logger,
		IDocumentMerger<OpenApiDocument> openApiDocumentMerger,
		IMergedDocumentSerializer<OpenApiDocument> documentSerializer,
		IOptions<KoalesceOpenApiOptions> providerOptions)
		: base(logger, providerOptions, openApiDocumentMerger, documentSerializer) { }
}