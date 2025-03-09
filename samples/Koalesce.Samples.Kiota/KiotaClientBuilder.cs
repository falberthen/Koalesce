using Kiota.Builder;
using Kiota.Builder.Configuration;
using Microsoft.Extensions.Logging;

namespace Koalesce.Samples.Kiota;


public class KiotaClientBuilder
{
	public static async Task BuildAsync(string openApiSpecPath)
	{
		// Define OpenAPI description URL or local file path
		string projectDirectory = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "Koalesce.Samples.Kiota"));
		string outputDirectory = Path.Combine(projectDirectory, "GeneratedClient");
		string clientNamespace = "Koalesce.Samples.Kiota.GeneratedClient";

		// Create a Kiota Configuration
		var config = new GenerationConfiguration
		{
			Language = GenerationLanguage.CSharp, // Target language
			OpenAPIFilePath = openApiSpecPath, // OpenAPI file or URL
			OutputPath = outputDirectory, // Output directory for generated client
			ClientClassName = "ApiClient", // Root class name of the generated client
			ClientNamespaceName = clientNamespace, // Namespace for the generated code
			IncludeAdditionalData = false, // Optional: Include additional data support
			Serializers = new HashSet<string>
			{
				"Microsoft.Kiota.Serialization.Json.JsonSerializationWriterFactory"
			},
			Deserializers = new HashSet<string>
			{
				"Microsoft.Kiota.Serialization.Json.JsonParseNodeFactory"
			}
		};

		// Create a logger instance for KiotaBuilder
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<KiotaBuilder>();

		// Create an HttpClient instance
		var httpClient = new HttpClient();

		// Create the KiotaBuilder instance with required parameters
		var kiotaBuilder = new KiotaBuilder(logger, config, httpClient);

		// Run Kiota code generation
		try
		{
			Console.WriteLine("Generating Kiota C# client...");
			await kiotaBuilder.GenerateClientAsync(CancellationToken.None);
			Console.WriteLine($"Client successfully generated in: {outputDirectory}");
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Kiota client generation failed: {ex.Message}");
		}

	}
}