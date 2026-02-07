namespace Koalesce.Core.Constants;

public static class CoreConstants
{
	public const string KoalesceClient = "KoalesceClient";
	public const int DefaultHttpTimeoutSeconds = 15;

	#region SchemaConflictPattern Placeholders
	public const string PrefixPlaceholder = "{Prefix}";
	public const string SchemaNamePlaceholder = "{SchemaName}";
	public const string DefaultSchemaConflictPattern = $"{PrefixPlaceholder}{SchemaNamePlaceholder}";
	#endregion

	#region KoalesceOptions Validation Messages
	public const string MergedEndpointCannotBeEmpty =
		"MergedEndpoint cannot be null or empty when using KoalesceMiddleware.";

	public const string ExcludePathCannotBeEmpty =
		"ExcludePaths[{0}] at Source index {1} cannot be empty.";

	public const string ExcludePathMustStartWithSlashOrWildcard =
		"ExcludePaths[{0}] at Source index {1} ('{2}') must start with '/' or '*'.";

	public const string ExcludePathInvalidWildcard =
		"ExcludePaths[{0}] at Source index {1} ('{2}') has invalid wildcard. Use single '*' per segment (e.g., '/api/*', '*/admin/*', '/*/health').";

	public const string SchemaConflictPatternValidationError =
		$"SchemaConflictPattern must contain both {PrefixPlaceholder} and {SchemaNamePlaceholder} placeholders.";

	public const string HttpTimeoutMustBePositive =
		"HttpTimeoutSeconds must be greater than zero.";

	public const string SourceMustHaveUrlOrFilePath =
		"Source at index {0} must have either Url or FilePath specified, but not both.";

	public const string SourceFilePathNotFound =
		"Source at index {0} has FilePath '{1}' that does not exist.";

	public const string DuplicateSourceFound =
		"Duplicate source '{0}' found at indices [{1}]. Each source must be unique.";
	#endregion
}