namespace Koalesce.Core;

/// <summary>
/// Contract defining document retrieval per implemented provider.
/// </summary>
public interface IKoalesceProvider
{
	/// <summary>
	/// Provides a serialized coalesced OpenAPI document
	/// </summary>
	/// <returns>The final result of merging API definitions.</returns>
	Task<string> ProvideSerializedDocumentAsync();
}
