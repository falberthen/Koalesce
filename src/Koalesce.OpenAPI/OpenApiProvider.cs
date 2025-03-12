namespace Koalesce.OpenAPI;

/// <summary>
/// OpenAPI provider for Koalesce.
/// </summary>
public class OpenApiProvider : KoalesceProviderBase<OpenApiOptions, OpenApiDocument>
{
	public OpenApiProvider(
		ILogger<OpenApiProvider> logger,
		IDocumentMerger<OpenApiDocument> openApiDocumentMerger,
		IMergedDocumentSerializer<OpenApiDocument> documentSerializer,
		IOptions<OpenApiOptions> providerOptions)
		: base(logger, providerOptions, openApiDocumentMerger, documentSerializer) { }
}