namespace Koalesce.Core.Tests.Helpers;

public static class ConfigurationHelper
{
	/// <summary>
	/// Converts a C# object into an IConfigurationRoot via JSON Stream.
	/// Simulates reading from appsettings.json
	/// </summary>
	public static IConfigurationRoot BuildConfigurationFromObject(object settings)
	{
		var jsonString = JsonSerializer.Serialize(settings);
		var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(jsonString));

		return new ConfigurationBuilder()
			.AddJsonStream(memoryStream)
			.Build();
	}
}
