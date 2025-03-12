namespace Koalesce.Core.Services;

/// <summary>
/// Contract for implementing serializers for merged documents.
/// </summary>
public interface IMergedDocumentSerializer<TMergeResult>
	where TMergeResult : class
{
	/// <summary>
	/// Serializes the merged document.
	/// </summary>
	string Serialize(TMergeResult mergedDocument);
}
