namespace Koalesce.OpenAPI;

/// <summary>
/// Provides a merged OpenAPI document for Koalesce.
/// </summary>
public class OpenApiProvider : OpenApiProviderBase<OpenApiOptions>, IKoalesceProvider
{
	public OpenApiProvider(
		ILogger<OpenApiProvider> logger,
		IOpenApiDocumentBuilder openApiDocumentBuilder,
		IOptions<OpenApiOptions> openApiOptions)
		: base(logger, openApiDocumentBuilder, openApiOptions)
	{
	}

	/// <summary>
	/// Override for ProvideSerializedDocumentAsync
	/// </summary>
	/// <returns>Serialized document</returns>
	public override async Task<string> ProvideSerializedDocumentAsync()
	{
		_logger.LogInformation("Loading routes from configuration from: {SourceOpenApiUrls}",
			_providerOptions.SourceOpenApiUrls);

		IEnumerable<string> apiUrls = _providerOptions.SourceOpenApiUrls;
		OpenApiDocument mergedDocument = await _openApiDocumentBuilder
			.BuildSingleDefinitionAsync(apiUrls);

		return SerializeOpenApiDocument(mergedDocument);		
	}
}
