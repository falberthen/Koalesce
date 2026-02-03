namespace Koalesce.Core.Options;

/// <summary>
/// Helper class for building type-safe validation paths for ValidationResult member names.
/// </summary>
internal static class ValidationPath
{
	/// <summary>
	/// Builds a path for a source at a specific index.
	/// Example: "Sources[0]"
	/// </summary>
	public static string Source(int index)
		=> $"{nameof(CoreOptions.Sources)}[{index}]";

	/// <summary>
	/// Builds a path for a source property at a specific index.
	/// Example: "Sources[0].Url"
	/// </summary>
	public static string Source(int index, string property)
		=> $"{nameof(CoreOptions.Sources)}[{index}].{property}";

	/// <summary>
	/// Builds a path for the Url property of a source.
	/// Example: "Sources[0].Url"
	/// </summary>
	public static string SourceUrl(int index)
		=> Source(index, nameof(ApiSource.Url));

	/// <summary>
	/// Builds a path for the FilePath property of a source.
	/// Example: "Sources[0].FilePath"
	/// </summary>
	public static string SourceFilePath(int index)
		=> Source(index, nameof(ApiSource.FilePath));

	/// <summary>
	/// Builds a path for an ExcludePath at specific indices.
	/// Example: "Sources[0].ExcludePaths[1]"
	/// </summary>
	public static string ExcludePath(int sourceIndex, int pathIndex)
		=> $"{nameof(CoreOptions.Sources)}[{sourceIndex}].{nameof(ApiSource.ExcludePaths)}[{pathIndex}]";
}
