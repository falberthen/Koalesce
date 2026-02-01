namespace Koalesce.Extensions;

public static class StringExtensions
{
	// Regex optimized for performance (compiled) to extract version from paths
	private static readonly Regex VersionRegex = new(@"/(?<version>v\d+)(/|$)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
	/// <summary>
	/// Extracts the API version (e.g., "v1", "v2") from the specified path.
	/// Returns "v1" if no version is found or the path is empty.
	/// </summary>
	public static string ExtractVersionFromPath(this string path)
	{
		if (string.IsNullOrEmpty(path))
			return KoalesceConstants.V1;

		var match = VersionRegex.Match(path);
		return match.Success
			? match.Groups["version"].Value
			: KoalesceConstants.V1;
	}


	// Regex to clean non-alphanumeric characters, allowing underscores
	private static readonly Regex _nonAlphaNumericRegex = new(@"[^a-zA-Z0-9_]", RegexOptions.Compiled);
	/// <summary>
	/// Removes all non-alphanumeric characters except underscores from the specified string.
	/// </summary>
	public static string CleanName(this string input)
	{
		if (string.IsNullOrEmpty(input))
			return string.Empty;

		return _nonAlphaNumericRegex.Replace(input, "");
	}

	/// <summary>
	/// Converts the first character of the specified string to uppercase, leaving the remaining characters unchanged.
	/// </summary>	
	public static string ToPascalCase(this string input)
	{
		if (string.IsNullOrEmpty(input))
			return string.Empty;

		return char.ToUpperInvariant(input[0]) + input[1..];
	}

	/// <summary>
	/// Determines the OpenAPI document format based on the file extension of the specified location.
	/// </summary>	
	public static string? GetFormatFromLocation(this string location)
	{
		var extension = Path.GetExtension(location).ToLowerInvariant();
		return extension switch
		{
			".yaml" or ".yml" => OpenApiConstants.Yaml,
			".json" => OpenApiConstants.Json,
			_ => null // Let the reader auto-detect
		};
	}
}