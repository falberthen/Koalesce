namespace Koalesce.OpenAPI.Extensions;

public static class StringExtensions
{
	// Regex optimized for performance (compiled) to extract version from paths
	private static readonly Regex VersionRegex = new(@"/(?<version>v\d+)(/|$)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
	public static string ExtractVersionFromPath(this string path)
	{
		if (string.IsNullOrEmpty(path))
			return OpenAPIConstants.V1;

		var match = VersionRegex.Match(path);
		return match.Success
			? match.Groups["version"].Value
			: OpenAPIConstants.V1;
	}


	// Regex to clean non-alphanumeric characters, allowing underscores
	private static readonly Regex _nonAlphaNumericRegex = new(@"[^a-zA-Z0-9_]", RegexOptions.Compiled);
	public static string CleanName(this string input)
	{
		if (string.IsNullOrEmpty(input))
			return string.Empty;

		return _nonAlphaNumericRegex.Replace(input, "");
	}

	public static string ToPascalCase(this string input)
	{
		if (string.IsNullOrEmpty(input))
			return string.Empty;

		return char.ToUpperInvariant(input[0]) + input[1..];
	}
}