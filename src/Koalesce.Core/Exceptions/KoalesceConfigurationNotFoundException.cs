namespace Koalesce.Core.Exceptions;

/// <summary>
/// Exception thrown when Koalesce configuration section is missing
/// </summary>
public class KoalesceConfigurationNotFoundException : Exception
{
	public KoalesceConfigurationNotFoundException() 
		: base("Koalesce configuration section was not found. Please check the documentation.") {}
}