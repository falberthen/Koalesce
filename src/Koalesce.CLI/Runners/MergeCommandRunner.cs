namespace Koalesce.CLI.Runners;

/// <summary>
/// Executes the OpenAPI merge operation using the provided configuration.
/// </summary>
public class MergeCommandRunner
{
	private readonly bool _verbose;
	private readonly bool _insecure;

	public MergeCommandRunner(bool verbose, bool insecure = false)
	{
		_verbose = verbose;
		_insecure = insecure;
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

			if (_insecure)			
				KoalesceConsoleUI.PrintWarning("SSL certificate validation is disabled (--insecure)");			

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

				// Remove noisy logs unless in verbose mode
				builder.AddFilter("Microsoft", LogLevel.Warning);
				builder.AddFilter("System", LogLevel.Warning);
				builder.AddFilter("Koalesce", _verbose ? LogLevel.Information : LogLevel.Warning);
			});

			services.AddSingleton<IMergedSpecificationWriter, MergedSpecificationWriter>();
			services.AddLogging();
			services.AddKoalesce(configuration, configureHttpClient: _insecure ? ConfigureInsecureHttpClient : null);

			using var provider = services.BuildServiceProvider();
			var mergeService = provider.GetRequiredService<IKoalesceMergeService>();
			var koalesceOptions = provider.GetRequiredService<IOptions<CoreOptions>>().Value;
			var writer = provider.GetRequiredService<IMergedSpecificationWriter>();
		
			KoalesceConsoleUI.PrintSourceList(koalesceOptions.Sources ?? Enumerable.Empty<ApiSource>());

			var mergedDefinition = await mergeService.MergeDefinitionsAsync();

			await writer.WriteAsync(outputPath, mergedDefinition);

			return 0;
		}
		catch (KoalesceConfigurationNotFoundException)
		{
			KoalesceConsoleUI.PrintError("Configuration Error", "Koalesce section not found in configuration file.");
			return 1;
		}
		catch (KoalesceInvalidConfigurationValuesException ex)
		{
			KoalesceConsoleUI.PrintError("Configuration Error", ex.Message);
			return 1;
		}
		catch (HttpRequestException ex)
		{
			KoalesceConsoleUI.PrintError("Network Error", $"Failed to fetch API specification: {ex.Message}");
			return 2;
		}
		catch (IOException ex)
		{
			KoalesceConsoleUI.PrintError("File Error", ex.Message);
			return 3;
		}
		catch (Exception ex)
		{
			KoalesceConsoleUI.PrintError("Unexpected Error", _verbose ? ex.ToString() : ex.Message);
			return 99;
		}
	}

	/// <summary>
	/// Configures the HttpClient to skip SSL certificate validation.
	/// </summary>
	private static void ConfigureInsecureHttpClient(IHttpClientBuilder builder)
	{
		builder.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
		{
			ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
		});
	}
}