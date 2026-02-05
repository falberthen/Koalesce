namespace Koalesce.Core.Services;

/// <summary>
/// Contract for the Koalesce merge service.
/// </summary>
public interface IKoalesceMergeService
{
	/// <summary>
	/// Merges API definitions and returns a detailed result including load status for each source.
	/// </summary>
	/// <param name="outputPath">Optional output path used to determine the serialization format (JSON/YAML).
	/// If not provided, falls back to MergedEndpoint from options.</param>
	/// <returns>A <see cref="MergeResult"/> containing the serialized document and source load results.</returns>
	Task<MergeResult> MergeDefinitionsAsync(string? outputPath = null);
}
