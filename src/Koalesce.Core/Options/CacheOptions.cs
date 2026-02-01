namespace Koalesce.Core.Options;

/// <summary>
/// Configuration settings for caching merged API definitions in Koalesce.
/// </summary>
public class CacheOptions
{
	/// <summary>
	/// The maximum duration (in seconds) that the merged OpenAPI document remains cached, 
	/// regardless of access. After this period, the cache is forcibly refreshed.
	/// Default: 24 hours
	/// </summary>
	public int AbsoluteExpirationSeconds { get; set; } = 86400;

	/// <summary>
	/// The sliding expiration time (in seconds) that resets the cache expiration 
	/// every time the merged document is accessed. If no access occurs before this period ends, 
	/// the cache expires earlier than its absolute expiration.
	/// Default: 5 minutes
	/// </summary>
	public int SlidingExpirationSeconds { get; set; } = 300;

	/// <summary>
	/// The minimum allowed expiration time (in seconds) for caching.
	/// Prevents excessively short cache durations that could cause unnecessary recomputation.
	/// Default: 30 seconds
	/// </summary>
	public int MinExpirationSeconds { get; set; } = 30; 

	/// <summary>
	/// Flag to disable caching entirely. If set to `true`, Koalesce will recompute the merged document on every request.
	/// Default: Caching enabled
	/// </summary>
	public bool DisableCache { get; set; } = false;
}