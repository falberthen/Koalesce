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

	/// <summary>
	/// Gets the displayable VirtualPrefix.
	/// </summary>
	public string? DisplayVirtualPrefix => Source.VirtualPrefix;

	/// <summary>
	/// Gets the displayable PrefixTagsWith.
	/// </summary>
	public string? DisplayPrefixTagsWith => Source.PrefixTagsWith;
}
