namespace Koalesce.Core.Services;

/// <summary>
/// Contract for implementing serializers for merged documents.
/// </summary>
public interface IMergedDocumentSerializer<TMergeResult>
	where TMergeResult : class
{
	/// <summary>
	/// Serializes an OpenAPI document using the configured format and version.
	/// </summary>
	string Serialize(TMergeResult mergedDocument);
}
