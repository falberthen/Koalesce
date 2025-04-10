namespace Koalesce.OpenAPI.CLI.Services;

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

		Directory.CreateDirectory(directory);
		await File.WriteAllTextAsync(outputPath, 
			content, 
			new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
		);

		Console.WriteLine($"\n🐨 Koalesced OpenAPI  written to: {Path.GetFullPath(outputPath)}");
	}
}