namespace Koalesce.Core.Exceptions;


/// <summary>
/// Exception thrown when a source API definition URL could not be reached or parsed,
/// and the strict validation (FailOnServiceLoadError) is enabled.
/// </summary>
public class KoalescePathCouldNotBeLoadedException : Exception
{
	/// <summary>
	/// The URL of the API source that caused the failure.
	/// </summary>
	public string? SourceUrl { get; }

	public KoalescePathCouldNotBeLoadedException()
		: base("One or more API sources could not be loaded.")
	{
	}

	public KoalescePathCouldNotBeLoadedException(string message)
		: base(message)
	{
	}

	public KoalescePathCouldNotBeLoadedException(string message, Exception innerException)
		: base(message, innerException)
	{
	}

	/// <summary>
	/// Initializes a new instance with the failing URL and the underlying error.
	/// </summary>
	public KoalescePathCouldNotBeLoadedException(string sourceUrl, string message, Exception? innerException = null)
		: base($"Failed to load API definition from '{sourceUrl}'. {message}", innerException)
	{
		SourceUrl = sourceUrl;
	}
}