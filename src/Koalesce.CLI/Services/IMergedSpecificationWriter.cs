namespace Koalesce.CLI.Services;

/// <summary>
/// Contract for service responsible for writting merged result on disc.
/// </summary>
public interface IMergedSpecificationWriter
{
	Task WriteMergeAsync(string outputPath, string content);
	Task WriteReportAsync(string? reportPath, MergeReport? report);
}
