/// <summary>
/// Entry point and command handler setup for the Koalesce OpenAPI CLI tool.
/// </summary>
public static class KoalesceCliApp
{
	public static async Task<int> RunAsync(string[] args)
	{
		Console.OutputEncoding = Encoding.UTF8;

		// Show banner when no arguments provided
		if (args.Length == 0)
		{
			KoalesceConsoleUI.PrintBanner();
		}

		RootCommand rootCommand = BuildRootCommand();

		// Options
		var outputOption = new Option<string>("--output", "-o")
		{
			Description = "Path to write the merged OpenAPI spec (e.g. apigateway.yaml)"
		};

		var configOption = new Option<string>("--config", "-c")
		{
			Description = "Path to the Koalesce configuration file"
		};

		var verboseOption = new Option<bool>("--verbose")
		{
			Description = "Enable verbose logging (show Information level logs)"
		};

		var insecureOption = new Option<bool>("--insecure", "-k", "-i")
		{
			Description = "Skip SSL certificate validation (use for self-signed certificates)"
		};

		var reportOption = new Option<string>("--report", "-r")
		{
			Description = "Path to write the merge report (e.g. report.html, report.json)"
		};

		rootCommand.Options.Add(outputOption);
		rootCommand.Options.Add(configOption);
		rootCommand.Options.Add(verboseOption);
		rootCommand.Options.Add(insecureOption);
		rootCommand.Options.Add(reportOption);

		// Setting handler
		rootCommand.SetAction(async (parseResult, cancellationToken) =>
		{
			string? output = parseResult.GetValue(outputOption);
			string? config = parseResult.GetValue(configOption);
			bool verbose = parseResult.GetValue(verboseOption);
			bool insecure = parseResult.GetValue(insecureOption);
			string? report = parseResult.GetValue(reportOption);

			if (string.IsNullOrWhiteSpace(output))
			{
				KoalesceConsoleUI.PrintError("--output (-o) is required");
				return 1;
			}

			if (string.IsNullOrWhiteSpace(config))
			{
				KoalesceConsoleUI.PrintError("--config (-c) is required");
				return 1;
			}

			var runner = new MergeCommandRunner(verbose, insecure);
			return await runner.RunAsync(output, config, report);
		});

		return await rootCommand.Parse(args).InvokeAsync();
	}

	/// <summary>
	/// Builds and returns the root command for the Koalesce CLI, including help text and usage examples.
	/// </summary>
	private static RootCommand BuildRootCommand() =>
		new(KoalesceConsoleUI.GetRootCommandDescription());
}
