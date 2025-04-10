namespace Koalesce.OpenAPI.CLI.Services;

/// <summary>
/// Contract for service responsible for writting merged result on disc.
/// </summary>
public interface IMergedSpecificationWriter
{
	Task WriteAsync(string outputPath, string content);
}
