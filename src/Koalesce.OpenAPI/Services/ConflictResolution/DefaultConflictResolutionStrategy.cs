namespace Koalesce.OpenAPI.Services.ConflictResolution;

/// <summary>
/// Defines a strategy for resolving schema conflicts by determining the appropriate resolution decision based on the
/// provided context.
/// </summary>
internal interface IConflictResolutionStrategy
{
	ResolutionDecision DetermineResolution(SchemaConflictContext context);
}

/// <summary>
/// Provides the default strategy for resolving schema key conflicts by determining how to rename or scope conflicting
/// schemas based on their virtual prefixes and conflict patterns.
/// </summary>
internal class DefaultConflictResolutionStrategy : IConflictResolutionStrategy
{
	public ResolutionDecision DetermineResolution(SchemaConflictContext context)
	{
		bool existingHasVirtualPrefix = !string.IsNullOrWhiteSpace(context.ExistingOrigin?.VirtualPrefix);
		bool currentHasVirtualPrefix = !string.IsNullOrWhiteSpace(context.CurrentVirtualPrefix);

		// Calculate potential new names
		string currentScopePrefix = SchemaNameUtils
			.DetermineSchemaScopePrefix(context.CurrentApiName, context.CurrentVirtualPrefix);
		string currentRenamedKey = SchemaNameUtils
			.ApplySchemaConflictPattern(context.ConflictPattern, context.SchemaKey, currentScopePrefix);

		// BOTH have VirtualPrefix -> Rename BOTH
		if (existingHasVirtualPrefix && currentHasVirtualPrefix)
		{
			string existingPrefix = SchemaNameUtils
				.DetermineSchemaScopePrefix(context.ExistingOrigin!.ApiName, context.ExistingOrigin.VirtualPrefix);
			string existingRenamedKey = SchemaNameUtils
				.ApplySchemaConflictPattern(context.ConflictPattern, context.SchemaKey, existingPrefix);

			return new ResolutionDecision(ConflictResolutionType.RenameBoth, currentRenamedKey, existingRenamedKey);
		}

		// Only EXISTING has VirtualPrefix -> Rename existing, current keeps original name
		// The existing schema explicitly requested namespacing, so we move it to make room for the new one.
		if (existingHasVirtualPrefix && !currentHasVirtualPrefix)
		{
			string existingPrefix = SchemaNameUtils
				.DetermineSchemaScopePrefix(context.ExistingOrigin!.ApiName, context.ExistingOrigin.VirtualPrefix);
			string existingRenamedKey = SchemaNameUtils
				.ApplySchemaConflictPattern(context.ConflictPattern, context.SchemaKey, existingPrefix);

			return new ResolutionDecision(ConflictResolutionType.RenameExisting, context.SchemaKey, existingRenamedKey);
		}

		// Fallback Strategy
		// If Only CURRENT has VirtualPrefix OR NEITHER has VirtualPrefix:
		// We default to renaming the current incoming schema to resolve the collision.
		return new ResolutionDecision(ConflictResolutionType.RenameCurrent, currentRenamedKey);
	}
}