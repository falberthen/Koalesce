namespace Koalesce.Services.ConflictResolution;

/// <summary>
/// Defines the action to be taken after analyzing a conflict.
/// </summary>
internal enum ConflictResolutionType
{
	RenameBoth,     // Rename both the existing schema in target and the incoming one
	RenameExisting, // Rename only the existing schema in target
	RenameCurrent,  // Rename only the incoming schema
	UseCurrentAsIs  // Overwrite or keep the incoming name
}

/// <summary>
/// Represents the final decision on how to handle the conflict.
/// </summary>
internal record ResolutionDecision(
	ConflictResolutionType Type,
	string NewCurrentKey,
	string? NewExistingKey = null
);
