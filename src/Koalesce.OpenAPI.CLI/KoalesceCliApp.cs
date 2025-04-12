namespace Koalesce.OpenAPI.CLI;

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

		var versionOption = new Option<bool>(
			"--v",
			description: "Show Koalesce CLI version and exit"
		);

		rootCommand.AddOption(outputOption);
		rootCommand.AddOption(configOption);
		rootCommand.AddOption(versionOption);

		// Setting handler
		rootCommand.SetHandler(async (InvocationContext context) =>
		{
			var showVersion = context.ParseResult.GetValueForOption(versionOption);
			if (showVersion)
			{
				KoalesceConsoleUI.PrintInfo($"Koalesce CLI v{GetVersionFromAssembly()}");								
				context.ExitCode = 0;
				return;
			}

			string? output = context.ParseResult.GetValueForOption(outputOption);
			string? config = context.ParseResult.GetValueForOption(configOption);

			if (string.IsNullOrWhiteSpace(output))
			{
				KoalesceConsoleUI.PrintError("Error: --output is required unless --v is specified");
				context.ExitCode = 1;
				return;
			}

			var runner = new MergeCommandRunner();
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

	/// <summary>
	/// Retrieves the CLI version from the assembly's informational version attribute.
	/// </summary>
	/// <returns>The version string of the Koalesce CLI.</returns>
	public static string GetVersionFromAssembly()
	{
		var version = Assembly
			.GetExecutingAssembly()
			.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
			?.InformationalVersion?
			.Split('+')[0];

		return version!;
	}
}