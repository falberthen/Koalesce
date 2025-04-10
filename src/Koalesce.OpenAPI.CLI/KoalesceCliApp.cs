namespace Koalesce.OpenAPI.CLI;

/// <summary>
/// Entry point and command handler setup for the Koalesce OpenAPI CLI tool.
/// </summary>
public static class KoalesceCliApp
{
	public static async Task<int> RunAsync(string[] args)
	{
		RootCommand rootCommand = BuildRootCommand();

		// Options
		var outputOption = new Option<string>(
			"--output",
			description: "Path to write the merged OpenAPI spec to (e.g. apigateway.yaml)")
		{
			IsRequired = false
		};

		var configOption = new Option<string>(
			"--config",
			description: "Path to the appsettings.json file to load Koalesce configuration from",
			getDefaultValue: () => "appsettings.json"
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
				Console.WriteLine($"Koalesce CLI {GetVersionFromAssembly()}");
				context.ExitCode = 0;
				return;
			}

			string? output = context.ParseResult.GetValueForOption(outputOption);
			string? config = context.ParseResult.GetValueForOption(configOption);

			if (string.IsNullOrWhiteSpace(output))
			{
				Console.Error.WriteLine("❌ Error: --output is required unless --v is specified.");
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
	private static RootCommand BuildRootCommand()
	{
		var rootCommand = new RootCommand("""
			🐨 Koalesce CLI for OpenAPI

			Examples:
			  koalesce --config appsettings.json --output merged.yaml
			  koalesce --v
			""");

		return rootCommand;
	}

	/// <summary>
	/// Retrieves the CLI version from the assembly's informational version attribute.
	/// </summary>
	/// <returns>The version string of the Koalesce CLI.</returns>
	private static string GetVersionFromAssembly()
	{
		var version = Assembly
			.GetExecutingAssembly()
			.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
			?.InformationalVersion?
			.Split('+')[0];

		return version!;
	}
}