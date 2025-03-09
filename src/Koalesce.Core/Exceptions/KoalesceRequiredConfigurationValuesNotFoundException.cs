namespace Koalesce.Core.Exceptions;

/// <summary>
/// Exception thrown when required configuration values are missing.
/// </summary>
public class KoalesceRequiredConfigurationValuesNotFoundException : Exception
{
	public KoalesceRequiredConfigurationValuesNotFoundException(string message) : base(message) { }

}