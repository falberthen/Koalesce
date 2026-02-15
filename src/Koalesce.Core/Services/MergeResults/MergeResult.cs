namespace Koalesce.Services.MergeResults;

/// <summary>
/// Represents the complete result of a merge operation, including
/// the serialized document and load status for each source.
/// </summary>
/// <param name="SerializedDocument">The serialized merged OpenAPI document.</param>
/// <param name="SourceResults">The load results for each configured source.</param>
/// <param name="Report">Optional structured report of the merge operation.</param>
public record MergeResult(string SerializedDocument, IReadOnlyList<SourceLoadResult> SourceResults, MergeReport? Report = null)
{
	/// <summary>
	/// Gets the sources that were successfully loaded.
	/// </summary>
	public IEnumerable<SourceLoadResult> LoadedSources =>
		SourceResults.Where(r => r.IsLoaded);

	/// <summary>
	/// Gets the sources that failed to load.
	/// </summary>
	public IEnumerable<SourceLoadResult> FailedSources =>
		SourceResults.Where(r => !r.IsLoaded);

	/// <summary>
	/// Gets the count of successfully loaded sources.
	/// </summary>
	public int LoadedCount =>
		SourceResults.Count(r => r.IsLoaded);

	/// <summary>
	/// Gets the count of sources that failed to load.
	/// </summary>
	public int FailedCount =>
		SourceResults.Count(r => !r.IsLoaded);
}
