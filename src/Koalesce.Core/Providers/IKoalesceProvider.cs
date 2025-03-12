namespace Koalesce.Core.Providers;

/// <summary>
/// Contract defining provider basic method.
/// </summary>
public interface IKoalesceProvider
{
	/// <summary>
	/// Provides a serialized coalesced document
	/// </summary>
	/// <returns>The final result of merging API definitions.</returns>
	Task<string> ProvideMergedDocumentAsync();
}