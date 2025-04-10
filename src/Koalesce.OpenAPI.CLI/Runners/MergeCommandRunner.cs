namespace Koalesce.OpenAPI.CLI.Runners;

/// <summary>
/// Executes the OpenAPI merge operation using the provided configuration.
/// </summary>
public class MergeCommandRunner
{
	/// <summary>
	/// Runs the OpenAPI merge process and writes the result to the specified output path.
	/// </summary>
	/// <param name="outputPath">The file path where the merged OpenAPI specification will be saved.</param>
	/// <param name="configPath">The path to the configuration file (e.g., appsettings.json) containing Koalesce settings.</param>
	/// <returns>The exit code indicating success (0) or failure (non-zero).</returns>
	public async Task<int> RunAsync(string outputPath, string configPath)
	{
		Console.WriteLine(@"
			 🐨 Koalesce CLI - for OpenAPI
			─────────────────────────────────────────────
			 Merging APIs with eucalyptus-fueled power!
			─────────────────────────────────────────────
		");

		if (!File.Exists(configPath))
		{
			Console.WriteLine($"❌ Configuration file not found: {Path.GetFullPath(configPath)}");
			return 1;
		}

		// Load appsettings.json config
		var configuration = new ConfigurationBuilder()
			.AddJsonFile(configPath, optional: false)
			.AddEnvironmentVariables()
			.Build();

		// Set up DI
		var services = new ServiceCollection();
		services.AddSingleton<IMergedSpecificationWriter, MergedSpecificationWriter>();
		services.AddLogging(builder => builder.AddConsole());
		services.AddKoalesce(configuration).ForOpenAPI();

		var provider = services.BuildServiceProvider();
		var openApiProvider = provider.GetRequiredService<OpenApiProvider>();
		var koalesceOptions = provider.GetRequiredService<IOptions<KoalesceOptions>>().Value;
		var writer = provider.GetRequiredService<IMergedSpecificationWriter>();

		// Add extension to output path if needed
		var ext = Path.GetExtension(koalesceOptions.MergedOpenApiPath ?? ".yaml");
		if (string.IsNullOrWhiteSpace(Path.GetExtension(outputPath)))
			outputPath += ext;

		// Show sources being merged
		Console.WriteLine($"🔍 Loaded {koalesceOptions.SourceOpenApiUrls?.Count ?? 0} source OpenAPI docs from config:");
		foreach (var path in koalesceOptions.SourceOpenApiUrls ?? Enumerable.Empty<string>())
		{
			Console.WriteLine($" • {path}");
		}
		Console.WriteLine();

		// Generate the merged spec
		var mergedSpec = await openApiProvider.ProvideMergedDocumentAsync();
		await writer.WriteAsync(outputPath, mergedSpec);

		return 0;
	}
}