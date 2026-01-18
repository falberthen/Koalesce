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
}