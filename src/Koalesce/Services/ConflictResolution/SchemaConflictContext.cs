namespace Koalesce.Services.ConflictResolution;

/// <summary>
/// Tracks the origin of a schema for conflict resolution.
/// </summary>
internal record SchemaOrigin(string ApiName, string? VirtualPrefix);

/// <summary>
/// Encapsulates the state required to make a decision about a schema conflict.
/// </summary>
internal record SchemaConflictContext(
	string SchemaKey,
	string CurrentApiName,
	string? CurrentVirtualPrefix,
	SchemaOrigin? ExistingOrigin,
	string ConflictPattern
);