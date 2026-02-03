namespace Koalesce.Core.Extensions;

public static class CoreOptionsExtensions
{
	/// <summary>
	/// Validation method for required configuration fields
	/// </summary>
	/// <param name="options"></param>
	/// <exception cref="KoalesceInvalidConfigurationValuesException"></exception>
	public static void Validate(this CoreOptions options)
	{
		var validationResults = new List<ValidationResult>();
		var context = new ValidationContext(options);
		bool isValid = Validator.TryValidateObject(options, context, validationResults, true);

		if (!isValid)
		{
			var errorMessages = string.Join("; ", validationResults.Select(v => v.ErrorMessage));
			throw new KoalesceInvalidConfigurationValuesException(
				$"Koalesce configuration is invalid: {errorMessages}"
			);
		}
	}
}
