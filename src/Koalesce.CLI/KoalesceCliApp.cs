/// <summary>
/// Entry point and command handler setup for the Koalesce OpenAPI CLI tool.
/// </summary>
public static class KoalesceCliApp
{
	public static async Task<int> RunAsync(string[] args)
	{
		Console.OutputEncoding = Encoding.UTF8;
		RootCommand rootCommand = BuildRootCommand();

		// Options
		var outputOption = new Option<string>(
			"--output",
			description: "Path to write the merged OpenAPI spec (e.g. apigateway.yaml)"
		);

		var configOption = new Option<string>(
			"--config",
			getDefaultValue: () => "appsettings.json",
			description: "Path to the Koalesce configuration file (default: appsettings.json)"
		);

		var verboseOption = new Option<bool>(
			"--verbose",
			description: "Enable verbose logging (show Information level logs)"
		);

		rootCommand.AddOption(outputOption);
		rootCommand.AddOption(configOption);
		rootCommand.AddOption(verboseOption);

		// Setting handler
		rootCommand.SetHandler(async (InvocationContext context) =>
		{
			string? output = context.ParseResult.GetValueForOption(outputOption);
			string? config = context.ParseResult.GetValueForOption(configOption);
			bool verbose = context.ParseResult.GetValueForOption(verboseOption);

			if (string.IsNullOrWhiteSpace(output))
			{
				KoalesceConsoleUI.PrintError("Error: --output is required unless --version is specified");
				context.ExitCode = 1;
				return;
			}

			var runner = new MergeCommandRunner(verbose);
			var exitCode = await runner.RunAsync(output, config ?? string.Empty);
			context.ExitCode = exitCode;
		});

		return await rootCommand.InvokeAsync(args);
	}

	/// <summary>
	/// Builds and returns the root command for the Koalesce CLI, including help text and usage examples.
	/// </summary>
	private static RootCommand BuildRootCommand() =>
		new RootCommand(KoalesceConsoleUI.GetRootCommandDescription());	
}