namespace Koalesce.Tests.Integration;

public class KoalesceForOpenApiCLITests : KoalesceIntegrationTestBase
{
	private static readonly string OutputFileName = $"apigateway-cli-output-{Guid.NewGuid()}.json";
	private static readonly string OutputPath = Path.Combine(Path.GetTempPath(), OutputFileName);
	private static readonly string ApiGatewaySettings = Path.Combine("RestAPIs", "appsettings.apigateway.json");

	[Fact]
	public async Task KoalesceCli_WhenRunWithValidConfig_ShouldMergeOpenAPIRoutes()
	{
		var configFullPath = Path.Combine(AppContext.BaseDirectory, ApiGatewaySettings);

		var koalescingApi = await StartWebApplicationAsync(ApiGatewaySettings,
			builder => builder.Services
				.AddKoalesce(builder.Configuration)
				.ForOpenAPI());

		if (File.Exists(OutputPath))
			File.Delete(OutputPath);

		var result = await RunKoalesceCliAsync(configFullPath, OutputPath);

		await koalescingApi.StopAsync();

		Assert.True(result.ExitCode == 0, $"CLI failed with exit code {result.ExitCode}.\nOutput:\n{result.Output}");
		Assert.True(File.Exists(OutputPath), "Output file was not created!");

		var content = await File.ReadAllTextAsync(OutputPath);
		Assert.Contains("openapi", content);
		Assert.Contains("/api/customers", content);
		Assert.Contains("/api/products", content);
		Assert.Contains("/inventory/api/products", content);
	}

	[Fact]
	public async Task KoalesceCli_WhenMissingConfig_ShouldFailGracefully()
	{
		var missingConfigPath = Path.Combine(Path.GetTempPath(), $"nonexistent-{Guid.NewGuid()}.json");

		var result = await RunKoalesceCliAsync(missingConfigPath, OutputPath);

		Assert.NotEqual(0, result.ExitCode);
		Assert.Contains("Configuration file not found", result.Output);
	}

	private async Task<(int ExitCode, string Output)> RunKoalesceCliAsync(string configPath, string outputPath)
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

	[Fact]
	public async Task KoalesceCli_WhenRunWithVersionCommand_ShouldDisplayVersionAndExit()
	{
		var cliDllPath = GetCliDllPath();

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

	private static string GetCliDllPath()
	{
		var repoRoot = GetSolutionRoot();
		var configurations = new[] { "Release", "Debug" };

		foreach (var config in configurations)
		{
			var candidate = Path.Combine(
				repoRoot,
				"src", "Koalesce.OpenAPI.CLI", "bin", config, "net10.0", "Koalesce.OpenAPI.CLI.dll"
			);

			if (File.Exists(candidate))
				return candidate;
		}

		throw new FileNotFoundException("Could not locate Koalesce.OpenAPI.CLI.dll in either Release or Debug configuration.");
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