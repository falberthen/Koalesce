namespace Koalesce.CLI.Services;

/// <summary>
/// Service responsible for writting results on disc.
/// </summary>
public class MergedSpecificationWriter : IMergedSpecificationWriter
{
	/// <inheritdoc/>
	public async Task WriteMergeAsync(string outputPath, string content)
	{
		var directory = Path.GetDirectoryName(outputPath);
		if (string.IsNullOrWhiteSpace(outputPath) || Path.GetInvalidPathChars().Any(outputPath.Contains))
			throw new InvalidOperationException($"Invalid output path: '{outputPath}'");

		if (!string.IsNullOrWhiteSpace(directory))
			Directory.CreateDirectory(directory);

		await WriteFileAsync(outputPath, async path =>
		{
			await File.WriteAllTextAsync(
				path,
				content,
				new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
			);
		}, "✅ Koalesced specifications to:");
	}

	/// <inheritdoc/>
	public async Task WriteReportAsync(string? reportPath, MergeReport? report)
	{
		if (reportPath is null || report is null)
			return;

		bool isHtml = reportPath.EndsWith(".html", StringComparison.OrdinalIgnoreCase);

		string content = isHtml
			? MergeReportHtmlRenderer.Render(report)
			: System.Text.Json.JsonSerializer.Serialize(report,
				new System.Text.Json.JsonSerializerOptions
				{
					PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
					DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
					WriteIndented = true
				});

		await WriteFileAsync(reportPath, async path =>
		{
			await File.WriteAllTextAsync(path, content);
		}, "📃 Merge report written to:");
	}

	private static async Task WriteFileAsync(
		string outputPath, Func<string, Task> writeAction, string successMessage)
	{
		try
		{
			await writeAction(outputPath);
			KoalesceConsoleUI.PrintSuccess(successMessage, Path.GetFullPath(outputPath));
		}
		catch (UnauthorizedAccessException ex)
		{
			KoalesceConsoleUI
				.PrintFileWrittingError("Permission denied when writing output file.", outputPath, ex);
			throw;
		}
		catch (IOException ex)
		{
			KoalesceConsoleUI
				.PrintFileWrittingError("I/O error occurred while writing output file.", outputPath, ex);
			throw;
		}
		catch (Exception ex)
		{
			KoalesceConsoleUI
				.PrintFileWrittingError("Unexpected error while writing output file.", outputPath, ex);
			throw;
		}
	}
}