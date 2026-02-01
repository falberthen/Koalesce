namespace Koalesce.CLI.Tests;

/// <summary>
/// Tests that require running APIs (integration tests with servers).
/// </summary>
[Collection("Koalesce.CLI Integration Tests")]
public class IntegrationTests : KoalesceIntegrationTestBase
{
	private static readonly string _outputFileName = $"apigateway-cli-output-{Guid.NewGuid()}.json";
	private static readonly string _outputPath = Path.Combine(Path.GetTempPath(), _outputFileName);
	private const string _appSettings = "RestAPIs/appsettings.openapi.json";

	[Fact]
	public async Task KoalesceForOpenAPICLI_WhenRunWithValidConfig_ShouldMergeOpenAPIRoutes()
	{
		var configFullPath = Path.Combine(AppContext.BaseDirectory, _appSettings);

		var koalescingApi = await StartWebApplicationAsync(_appSettings,
			builder => builder.Services
				.AddKoalesce(builder.Configuration));

		if (File.Exists(_outputPath))
			File.Delete(_outputPath);

		var result = await CliTestHelpers.RunKoalesceCliAsync(configFullPath, _outputPath);

		await koalescingApi.StopAsync();

		Assert.True(result.ExitCode == 0, $"CLI failed with exit code {result.ExitCode}.\nOutput:\n{result.Output}");
		Assert.True(File.Exists(_outputPath), "Output file was not created!");

		var content = await File.ReadAllTextAsync(_outputPath);
		Assert.Contains("openapi", content);
		Assert.Contains("/api/customers", content);
		Assert.Contains("/api/products", content);
	}
}

/// <summary>
/// Standalone CLI tests that don't require running servers.
/// </summary>
public class CliStandaloneTests
{
	private static readonly string _outputPath = Path.Combine(Path.GetTempPath(), $"cli-test-{Guid.NewGuid()}.json");

	[Fact]
	public async Task KoalesceForOpenAPICLI_WhenMissingConfig_ShouldFailGracefully()
	{
		var missingConfigPath = Path.Combine(Path.GetTempPath(), $"nonexistent-{Guid.NewGuid()}.json");

		var result = await CliTestHelpers.RunKoalesceCliAsync(missingConfigPath, _outputPath);

		Assert.NotEqual(0, result.ExitCode);
		Assert.Contains("Configuration file not found", result.Output);
	}

	[Fact]
	public async Task KoalesceForOpenAPICLI_WhenRunWithVersionCommand_ShouldDisplayVersionAndExit()
	{
		var cliDllPath = CliTestHelpers.GetCliDllPath();

		var psi = new ProcessStartInfo
		{
			FileName = "dotnet",
			Arguments = $"\"{cliDllPath}\" --version",
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true,
		};

		using var process = Process.Start(psi)!;

		string output = await process.StandardOutput.ReadToEndAsync();
		string error = await process.StandardError.ReadToEndAsync();

		await process.WaitForExitAsync();
		var combinedOutput = output + error;

		var expectedVersion = typeof(KoalesceCliApp).Assembly
			.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
			.InformationalVersion?
			.Split('+')[0];

		Assert.Contains(expectedVersion!, combinedOutput);
	}
}

/// <summary>
/// Shared helper methods for CLI tests.
/// </summary>
internal static class CliTestHelpers
{
	public static async Task<(int ExitCode, string Output)> RunKoalesceCliAsync(string configPath, string outputPath)
	{
		var cliDllPath = GetCliDllPath();
		var psi = new ProcessStartInfo
		{
			FileName = "dotnet",
			ArgumentList =
			{
				cliDllPath,
				"--config", configPath,
				"--output", outputPath
			},
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true,
		};

		psi.EnvironmentVariables["NO_COLOR"] = "true";
		psi.EnvironmentVariables["DOTNET_NOLOGO"] = "true";

		using var process = Process.Start(psi)!;
		var outputTask = process.StandardOutput.ReadToEndAsync();
		var errorTask = process.StandardError.ReadToEndAsync();

		await Task.WhenAll(outputTask, errorTask);
		await process.WaitForExitAsync();

		return (process.ExitCode, outputTask.Result + errorTask.Result);
	}

	public static string GetCliDllPath()
	{
		var repoRoot = GetSolutionRoot();
		var configurations = new[] { "Release", "Debug" };

		foreach (var config in configurations)
		{
			var candidate = Path.Combine(
				repoRoot,
				"src", "Koalesce.CLI", "bin", config, "net10.0", "Koalesce.CLI.dll"
			);

			if (File.Exists(candidate))
				return candidate;
		}

		throw new FileNotFoundException("Could not locate Koalesce.CLI.dll in either Release or Debug configuration.");
	}

	private static string GetSolutionRoot()
	{
		var dir = new DirectoryInfo(AppContext.BaseDirectory);

		while (dir != null && !Directory.GetFiles(dir.FullName, "*.sln").Any())
		{
			dir = dir.Parent;
		}

		if (dir == null)
			throw new DirectoryNotFoundException("Could not locate the solution root (.sln file).");

		return dir.FullName;
	}
}