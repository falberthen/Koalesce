namespace Koalesce.CLI.UI;

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
		Console.WriteLine($"{Cyan}│               🐨 Koalesce CLI                │{Reset}");
		Console.WriteLine($"{Cyan}├──────────────────────────────────────────────┤{Reset}");
		Console.WriteLine($"{Cyan}│  Merging APIs with eucalyptus-fueled power!  │{Reset}");
		Console.WriteLine($"{Cyan}╰──────────────────────────────────────────────╯{Reset}");
	}

	/// <summary>
	/// Returns 
	/// </summary>
	/// <returns></returns>
	public static string GetRootCommandDescription() =>
		"🐨 Koalesce CLI";

	/// <summary>
	/// Prints an error message
	/// </summary>
	/// <param name="errorMessage"></param>
	public static void PrintError(string errorMessage) =>
		Console.WriteLine($"\n{Red}❌ {errorMessage}{Reset}\n");

	/// <summary>
	/// Prints a categorized error message with a title and details.
	/// </summary>
	/// <param name="category">The error category (e.g., "Configuration Error", "Network Error").</param>
	/// <param name="message">The detailed error message.</param>
	public static void PrintError(string category, string message)
	{
		Console.WriteLine($"\n{Red}❌ [{category}]{Reset}");
		Console.WriteLine($"{Red}   {message}{Reset}\n");
	}

	/// <summary>
	/// Prints a success message
	/// </summary>
	/// <param name="successMessage"></param>
	public static void PrintSuccess(string successMessage) =>
		Console.WriteLine($"\n{Green}✅ {successMessage}{Reset}\n");

	/// <summary>
	/// Prints a success message with a highlighted path
	/// </summary>
	public static void PrintSuccess(string message, string path) =>
		Console.WriteLine($"\n{Green}✅ {message}{Reset} {path}\n");

	/// <summary>
	/// Prints an informational message
	/// </summary>
	/// <param name="infoMessage"></param>
	public static void PrintInfo(string infoMessage) =>
		Console.WriteLine($"\n{Yellow}ℹ️ {infoMessage}{Reset}\n");

	/// <summary>
	/// Prints a warning message
	/// </summary>
	/// <param name="warningMessage"></param>
	public static void PrintWarning(string warningMessage) =>
		Console.WriteLine($"\n{Yellow}⚠️  {warningMessage}{Reset}");

	/// <summary>
	/// Prints an error message indicating the configuration file was not found.
	/// </summary>
	/// <param name="configPath">The path to the missing configuration file.</param>
	public static void PrintMissingConfigError(string configPath)
	{
		Console.WriteLine($"{Red}❌ Configuration file not found:\x1b[0m {Path.GetFullPath(configPath)}{Reset}\n");
	}

	/// <summary>
	/// Prints the list of source load results with status indicators.
	/// </summary>
	/// <param name="sourceResults">The load results for each source.</param>
	public static void PrintSourceResults(IReadOnlyList<SourceLoadResult> sourceResults)
	{
		int loadedCount = sourceResults.Count(r => r.IsLoaded);
		Console.WriteLine($"\n{Cyan}🔍 Loaded {loadedCount} OpenAPI specifications:{Reset}\n");

		foreach (var result in sourceResults)
		{
			string displayPrefix = "";

			if (!string.IsNullOrEmpty(result.Source.VirtualPrefix))
			{
				var cleanPrefix = result.Source.VirtualPrefix.Trim('/');
				displayPrefix = $"{Magenta}[/{cleanPrefix}]{Reset} ";
			}

			string apiPath = result.DisplayPath;
			string statusIndicator = result.IsLoaded ? "" : $" {Red}[Not loaded]{Reset}";

			Console.WriteLine($" {Green}•{Reset} {displayPrefix}{apiPath}{statusIndicator}");
		}

		Console.WriteLine();
	}

	/// <summary>
	/// Prints an error message indicating problems when writting merged specification file.
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