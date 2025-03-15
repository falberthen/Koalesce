namespace Koalesce.Core.Exceptions;

/// <summary>
/// If KoalesceConfiguration has SkipIdenticalPaths = false, 
/// Koalesce thrown this exception when merging documents with identical paths
/// </summary>
public class KoalesceIdenticalPathFoundException : Exception
{	
	/// <summary>
	/// Initializes a new instance of the exception for multiple identical paths.
	/// </summary>
	/// <param name="paths">A collection of conflicting API paths.</param>
	public KoalesceIdenticalPathFoundException(IEnumerable<(string Path, string ApiName)> paths)
		: base($"Identical paths detected: {string.Join(", ", paths.Select(p => $"'{p.Path}' from API '{p.ApiName}'"))}.") {}
}
