namespace Koalesce.CLI.Services;

/// <summary>
/// Service responsible for writting merged result on disc.
/// </summary>
public class MergedSpecificationWriter : IMergedSpecificationWriter
{
	/// <inheritdoc/>
	public async Task WriteAsync(string outputPath, string content)
	{
		var directory = Path.GetDirectoryName(outputPath);
		if (string.IsNullOrWhiteSpace(outputPath) || Path.GetInvalidPathChars().Any(outputPath.Contains))
			throw new InvalidOperationException($"Invalid output path: '{outputPath}'");

		try
		{
			if (!string.IsNullOrWhiteSpace(directory))
				Directory.CreateDirectory(directory);

			await File.WriteAllTextAsync(
				outputPath,
				content,
				new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
			);			

			KoalesceConsoleUI
				.PrintSuccess("Koalesced OpenAPI definitions to:", Path.GetFullPath(outputPath));
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