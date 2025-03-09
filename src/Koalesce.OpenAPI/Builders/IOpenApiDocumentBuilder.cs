namespace Koalesce.OpenAPI.Builders;

/// <summary>
/// Builds a single API definition document from multiple API specifications
/// </summary>
public interface IOpenApiDocumentBuilder
{
	Task<OpenApiDocument> BuildSingleDefinitionAsync(IEnumerable<string> apiUrls);
}
