namespace Koalesce.Core.Services;

/// <summary>
/// Contract for providers implementing document mergers
/// with returning a single API definition document from multiple API specifications
/// </summary>
public interface IDocumentMerger<T>
	where T : class
{
	Task<T> MergeIntoSingleDefinitionAsync();
}
