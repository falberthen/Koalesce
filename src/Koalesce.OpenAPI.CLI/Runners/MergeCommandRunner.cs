namespace Koalesce.OpenAPI.CLI.Runners;

/// <summary>
/// Executes the OpenAPI merge operation using the provided configuration.
/// </summary>
public class MergeCommandRunner
{
	private readonly bool _verbose;

	public MergeCommandRunner(bool verbose)
	{
		_verbose = verbose;
	}

	/// <summary>
	/// Runs the OpenAPI merge process and writes the result to the specified output path.
	/// </summary>
	/// <param name="outputPath">The file path where the merged OpenAPI specification will be saved.</param>
	/// <param name="configPath">The path to the configuration file (e.g., appsettings.json) containing Koalesce settings.</param>
	/// <returns>The exit code indicating success (0) or failure (non-zero).</returns>
	public async Task<int> RunAsync(string outputPath, string configPath)
	{
		try
		{
			KoalesceConsoleUI.PrintBanner();

			if (!File.Exists(configPath))
			{
				KoalesceConsoleUI.PrintMissingConfigError(configPath);
				return 1;
			}

			// Load configuration and set up DI
			var configuration = new ConfigurationBuilder()
				.AddJsonFile(configPath, optional: false)
				.AddEnvironmentVariables()
				.Build();

			var services = new ServiceCollection();

			services.AddLogging(builder =>
			{
				builder.AddConsole();				
				builder.SetMinimumLevel(_verbose ? LogLevel.Information : LogLevel.Warning);

				// Remote noisy logs unless in verbose mode
				builder.AddFilter("Microsoft", LogLevel.Warning);
				builder.AddFilter("System", LogLevel.Warning);
				builder.AddFilter("Koalesce", _verbose ? LogLevel.Information : LogLevel.Warning);
			});

			services.AddSingleton<IMergedSpecificationWriter, MergedSpecificationWriter>();
			services.AddLogging();
			services.AddKoalesce(configuration).ForOpenAPI();

			using var provider = services.BuildServiceProvider();
			var openApiProvider = provider.GetRequiredService<OpenApiProvider>();
			var koalesceOptions = provider.GetRequiredService<IOptions<KoalesceOptions>>().Value;
			var writer = provider.GetRequiredService<IMergedSpecificationWriter>();

			if (string.IsNullOrWhiteSpace(Path.GetExtension(outputPath)))
				outputPath += Path.GetExtension(koalesceOptions.MergedDocumentPath ?? ".yaml");

			KoalesceConsoleUI.PrintSourceList(koalesceOptions.Sources ?? Enumerable.Empty<SourceDefinition>());

			var mergedSpec = await openApiProvider.ProvideMergedDocumentAsync();
			await writer.WriteAsync(outputPath, mergedSpec);

			return 0;
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.Message);
			return 2;
		}
	}
}