namespace Koalesce.Services.MergeResults;

/// <summary>
/// Represents the result of loading a single API source.
/// </summary>
/// <param name="Source">The API source that was loaded.</param>
/// <param name="IsLoaded">Whether the source was successfully loaded.</param>
/// <param name="ErrorMessage">Optional error message if loading failed.</param>
public record SourceLoadResult(ApiSource Source, bool IsLoaded, string? ErrorMessage = null)
{
	/// <summary>
	/// Gets the display path for this source (URL or file path).
	/// </summary>
	public string DisplayPath => Source.Url ?? Source.FilePath ?? "Unknown";
}
