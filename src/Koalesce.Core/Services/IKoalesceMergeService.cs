namespace Koalesce.Core.Services;

/// <summary>
/// Contract for the Koalesce merge service.
/// </summary>
public interface IKoalesceMergeService
{
	/// <summary>
	/// Merges API definitions and returns the serialized result.
	/// </summary>
	/// <param name="outputPath">Optional output path used to determine the serialization format (JSON/YAML).
	/// If not provided, falls back to MergedEndpoint from options.</param>
	Task<string> MergeDefinitionsAsync(string? outputPath = null);
}
