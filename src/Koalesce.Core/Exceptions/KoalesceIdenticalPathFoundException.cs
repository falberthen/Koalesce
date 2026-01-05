namespace Koalesce.Core.Exceptions;

/// <summary>
/// Exception thrown when a duplicate path is detected during the merge process
/// If KoalesceConfiguration has SkipIdenticalPaths = false, 
/// </summary>
public class KoalesceIdenticalPathFoundException : Exception
{
	public string Path { get; }
	public string ApiName { get; }

	/// <summary>
	/// Initializes a new instance of the exception for a single conflicting path.
	/// </summary>
	/// <param name="path">The specific path that caused the collision (e.g., "/api/products").</param>
	/// <param name="apiName">The name of the API source where the duplicate was found.</param>
	public KoalesceIdenticalPathFoundException(string path, string apiName)
		: base($"Identical path '{path}' detected while merging API '{apiName}'. " +
			   $"To resolve this, configure a unique 'Prefix' for this source or enable 'SkipIdenticalPaths'.")
	{
		Path = path;
		ApiName = apiName;
	}
}