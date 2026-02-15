namespace Koalesce.Services.MergeResults;

/// <summary>
/// Structured report summarizing everything that happened during a merge operation.
/// Sections are nullable — only populated when something noteworthy occurred.
/// </summary>
public record MergeReport
{
	public DateTimeOffset Timestamp { get; init; }
	public MergeReportSummary Summary { get; init; } = new();
	public List<MergeReportSource> Sources { get; init; } = [];
	public MergeReportConflicts? Conflicts { get; init; }
	public MergeReportDeduplication? Deduplication { get; init; }
	public MergeReportRemovals? Removals { get; init; }
}

public record MergeReportSummary
{
	public int TotalSources { get; init; }
	public int LoadedSources { get; init; }
	public int? FailedSources { get; init; }
	public int? TotalPathsMerged { get; init; }
	public int? PathsExcluded { get; init; }
	public int? PathsSkipped { get; init; }
	public int? SchemaConflictsResolved { get; init; }
	public int? SecuritySchemeConflictsResolved { get; init; }
	public int? SecuritySchemesDeduplicated { get; init; }
}

public record MergeReportSource
{
	public string Path { get; init; } = string.Empty;
	public string? VirtualPrefix { get; init; }
	public string? PrefixTagsWith { get; init; }
	public bool IsLoaded { get; init; }
	public string? ErrorMessage { get; init; }
}

public record MergeReportConflicts
{
	public List<MergeReportSchemaConflict>? Schemas { get; init; }
	public List<MergeReportSecuritySchemeConflict>? SecuritySchemes { get; init; }
}

public record MergeReportSchemaConflict
{
	public string OriginalKey { get; init; } = string.Empty;
	public string NewKey { get; init; } = string.Empty;
	public string Resolution { get; init; } = string.Empty;
	public string SourceApi { get; init; } = string.Empty;
}

public record MergeReportSecuritySchemeConflict
{
	public string OriginalKey { get; init; } = string.Empty;
	public string NewKey { get; init; } = string.Empty;
	public string Resolution { get; init; } = string.Empty;
	public string SourceApi { get; init; } = string.Empty;
}

public record MergeReportDeduplication
{
	public List<MergeReportDeduplicatedScheme> SecuritySchemes { get; init; } = [];
}

public record MergeReportDeduplicatedScheme
{
	public string Key { get; init; } = string.Empty;
	public string SourceApi { get; init; } = string.Empty;
}

public record MergeReportRemovals
{
	public List<MergeReportExcludedPath>? ExcludedPaths { get; init; }
	public List<MergeReportSkippedPath>? SkippedPaths { get; init; }
}

public record MergeReportExcludedPath
{
	public string Path { get; init; } = string.Empty;
	public string Api { get; init; } = string.Empty;
	public string Pattern { get; init; } = string.Empty;
}

public record MergeReportSkippedPath
{
	public string Path { get; init; } = string.Empty;
	public string Api { get; init; } = string.Empty;
}
