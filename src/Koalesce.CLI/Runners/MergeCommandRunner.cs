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
	public async Task<int> RunAsync(string outputPath, string configPath, string? reportPath = null)
	{
		try
		{
			KoalesceConsoleUI.PrintBanner();

			if (_insecure)
				KoalesceConsoleUI.PrintWarning("SSL certificate validation is disabled (--insecure)");

			configPath = Path.GetFullPath(configPath);
			outputPath = Path.GetFullPath(outputPath);
			reportPath = reportPath is not null ? Path.GetFullPath(reportPath) : null;

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
				if (_verbose)
				{
					builder.AddConsole();
					builder.SetMinimumLevel(LogLevel.Information);
				}
				else
				{
					// Suppress ALL logs in non-verbose mode - errors shown via CLI output only
					builder.SetMinimumLevel(LogLevel.None);
				}
			});

			services.AddSingleton<IMergedSpecificationWriter, MergedSpecificationWriter>();
			services.AddKoalesce(configuration, configureHttpClient: _insecure ? ConfigureInsecureHttpClient : null);

			using var provider = services.BuildServiceProvider();
			var mergeService = provider.GetRequiredService<IKoalesceMergeService>();
			var writer = provider.GetRequiredService<IMergedSpecificationWriter>();

			// Merge specifications and get results with load status
			var result = await mergeService.MergeSpecificationsAsync(outputPath);

			// Print source list with load status indicators
			KoalesceConsoleUI.PrintSourceResults(result.SourceResults);

			// Write merged specification to output path
			await writer.WriteMergeAsync(outputPath, result.SerializedDocument);

			// Optionally write a report with details about the merge process
			await writer.WriteReportAsync(reportPath, result.Report);

			KoalesceConsoleUI.PrintBlankLine();
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
			// Never show stack traces in CLI - only the error message
			KoalesceConsoleUI.PrintError("Unexpected Error", ex.Message);
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