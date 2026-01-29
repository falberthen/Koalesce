namespace Koalesce.OpenAPI.CLI.UI;

/// <summary>
/// Provides user-facing console output for the Koalesce CLI
/// </summary>
public static class KoalesceConsoleUI
{
	/// <summary>
	/// ANSI colors 
	/// </summary>
	public const string Red = "\x1b[1;31m";
	public const string Cyan = "\x1b[36m";
	public const string Green = "\x1b[32m";
	public const string Yellow = "\x1b[33m";
	public const string Magenta = "\x1b[35m";
	public const string Bold = "\x1b[1m";
	public const string Reset = "\x1b[0m";

	/// <summary>
	/// Prints the main Koalesce CLI banner with ASCII art and color formatting.
	/// </summary>
	public static void PrintBanner()
	{
		Console.WriteLine($"{Cyan}╭──────────────────────────────────────────────╮{Reset}");
		Console.WriteLine($"{Cyan}│        🐨 Koalesce CLI for OpenAPI           │{Reset}");
		Console.WriteLine($"{Cyan}├──────────────────────────────────────────────┤{Reset}");
		Console.WriteLine($"{Cyan}│  Merging APIs with eucalyptus-fueled power!  │{Reset}");
		Console.WriteLine($"{Cyan}╰──────────────────────────────────────────────╯{Reset}\n");		
	}

	/// <summary>
	/// Returns 
	/// </summary>
	/// <returns></returns>
	public static string GetRootCommandDescription() =>
		"🐨 Koalesce CLI for OpenAPI";

	/// <summary>
	/// Prints an error message
	/// </summary>
	/// <param name="errorMessage"></param>
	public static void PrintError(string errorMessage) =>
		Console.WriteLine($"\n{Red}❌ {errorMessage}{Reset}\n");

	/// <summary>
	/// Prints a success message
	/// </summary>
	/// <param name="successMessage"></param>
	public static void PrintSuccess(string successMessage) =>
		Console.WriteLine($"\n{Green}✅ {successMessage}{Reset}\n");

	/// <summary>
	/// Prints an informational message
	/// </summary>
	/// <param name="infoMessage"></param>
	public static void PrintInfo(string infoMessage) =>
		Console.WriteLine($"\n{Yellow}ℹ️ {infoMessage}{Reset}\n");

	/// <summary>
	/// Prints an error message indicating the configuration file was not found.
	/// </summary>
	/// <param name="configPath">The path to the missing configuration file.</param>
	public static void PrintMissingConfigError(string configPath)
	{
		Console.WriteLine($"{Red}❌ Configuration file not found:\x1b[0m {Path.GetFullPath(configPath)}{Reset}\n");
	}

	/// <summary>
	/// Prints the list of OpenAPI source document URLs loaded from configuration.
	/// </summary>
	/// <param name="sources">The list of OpenAPI URLs.</param>
	public static void PrintSourceList(IEnumerable<ApiSource> sources)
	{
		Console.WriteLine($"{Cyan}🔍 Loaded {sources.Count()} OpenAPI definitions from config:{Reset}\n");
		foreach (var source in sources)
		{			
			string displayPrefix = "";

			if (!string.IsNullOrEmpty(source.VirtualPrefix))
			{
				var cleanPrefix = source.VirtualPrefix.Trim('/');
				displayPrefix = $"{Magenta}[/{cleanPrefix}]{Reset} ";
			}
			
			string apiPath = source.Url ?? source.FilePath!;
			Console.WriteLine($" {Green}•{Reset} {displayPrefix}{apiPath}");
		}

		Console.WriteLine();
	}

	/// <summary>
	/// Prints an error message indicating problems when writting merged file.
	/// </summary>
	/// <param name="message">A short description of the error that occurred.</param>
	/// <param name="path">The file or resource path related to the error.</param>
	/// <param name="ex">The exception instance containing details about the failure.</param>
	public static void PrintFileWrittingError(string message, string path, Exception ex)
	{
		Console.WriteLine($"\n{Red}❌ {message}{Reset}");
		Console.WriteLine($"{Red} → {path}{Reset}");
		Console.WriteLine($"{Red} → {ex.Message}{Reset}\n");
	}
}