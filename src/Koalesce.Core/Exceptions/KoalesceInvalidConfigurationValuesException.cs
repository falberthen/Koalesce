namespace Koalesce.Core.Exceptions;

/// <summary>
/// Exception thrown Koalesce configuration is invalid
/// </summary>
public class KoalesceInvalidConfigurationValuesException : Exception
{
	public KoalesceInvalidConfigurationValuesException(string message) : base(message) { }

}