using Microsoft.Extensions.Configuration;

namespace Koalesce.Tests.Unit;

/// <summary>
/// Base class for Koalesce unit tests.
/// </summary>
[Collection("Koalesce Unit Tests")]
public abstract class KoalesceUnitTestBase 
{
	/// <summary>
	/// Loads a test JSON configuration file.
	/// </summary>
	public IConfiguration LoadConfigurations(string fileName) =>
		new ConfigurationBuilder()
			.SetBasePath(Directory.GetCurrentDirectory())
			.AddJsonFile(fileName, optional: false, reloadOnChange: true)
			.Build();
}
