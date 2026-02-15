namespace Koalesce.Services.Report;

/// <summary>
/// Mutable accumulator for building a <see cref="MergeReport"/> during the merge process.
/// Created per merge invocation — not registered in DI.
/// </summary>
internal class MergeReportBuilder
{
	private readonly DateTimeOffset _timestamp = DateTimeOffset.UtcNow;

	private readonly List<MergeReportSource> _sources = [];
	private readonly List<MergeReportSchemaConflict> _schemaConflicts = [];
	private readonly List<MergeReportSecuritySchemeConflict> _securitySchemeConflicts = [];
	private readonly List<MergeReportDeduplicatedScheme> _deduplicatedSchemes = [];
	private readonly List<MergeReportExcludedPath> _excludedPaths = [];
	private readonly List<MergeReportSkippedPath> _skippedPaths = [];
	private int _totalPathsMerged;

	public void AddSource(SourceLoadResult result) =>
		_sources.Add(new MergeReportSource
		{
			Path = result.DisplayPath,
			VirtualPrefix = result.DisplayVirtualPrefix,
			PrefixTagsWith = result.DisplayPrefixTagsWith,
			IsLoaded = result.IsLoaded,
			ErrorMessage = result.ErrorMessage
		});

	public void AddSchemaConflict(
		string originalKey, string newKey,
		string resolution, string sourceApi) =>
		_schemaConflicts.Add(new MergeReportSchemaConflict
		{
			OriginalKey = originalKey,
			NewKey = newKey,
			Resolution = resolution,
			SourceApi = sourceApi
		});

	public void AddSecuritySchemeConflict(
		string originalKey, string newKey,
		string resolution, string sourceApi) =>
		_securitySchemeConflicts.Add(new MergeReportSecuritySchemeConflict
		{
			OriginalKey = originalKey,
			NewKey = newKey,
			Resolution = resolution,
			SourceApi = sourceApi
		});

	public void AddDeduplicatedSecurityScheme(string key, string sourceApi) =>
		_deduplicatedSchemes.Add(new MergeReportDeduplicatedScheme
		{
			Key = key,
			SourceApi = sourceApi
		});

	public void AddExcludedPath(string path, string api, string pattern) =>
		_excludedPaths.Add(new MergeReportExcludedPath
		{
			Path = path,
			Api = api,
			Pattern = pattern
		});

	public void AddSkippedPath(string path, string api) =>
		_skippedPaths.Add(new MergeReportSkippedPath
		{
			Path = path,
			Api = api
		});

	public void IncrementPathsMerged() =>
		_totalPathsMerged++;

	/// <summary>
	/// Builds the final report. Only populates sections and counts that have data,
	/// so the serialized JSON stays clean — no noise from empty sections or zero counts.
	/// </summary>
	public MergeReport Build()
	{
		int failedCount = _sources.Count(s => !s.IsLoaded);

		bool hasConflicts = _schemaConflicts.Count > 0 || _securitySchemeConflicts.Count > 0;
		bool hasDedup = _deduplicatedSchemes.Count > 0;
		bool hasRemovals = _excludedPaths.Count > 0 || _skippedPaths.Count > 0;

		return new MergeReport
		{
			Timestamp = _timestamp,
			Summary = new MergeReportSummary
			{
				TotalSources = _sources.Count,
				LoadedSources = _sources.Count(s => s.IsLoaded),
				FailedSources = NullIfZero(failedCount),
				TotalPathsMerged = NullIfZero(_totalPathsMerged),
				PathsExcluded = NullIfZero(_excludedPaths.Count),
				PathsSkipped = NullIfZero(_skippedPaths.Count),
				SchemaConflictsResolved = NullIfZero(_schemaConflicts.Count),
				SecuritySchemeConflictsResolved = NullIfZero(_securitySchemeConflicts.Count),
				SecuritySchemesDeduplicated = NullIfZero(_deduplicatedSchemes.Count)
			},
			Sources = [.. _sources],
			Conflicts = hasConflicts ? new MergeReportConflicts
			{
				Schemas = _schemaConflicts.Count > 0 ? [.. _schemaConflicts] : null,
				SecuritySchemes = _securitySchemeConflicts.Count > 0 ? [.. _securitySchemeConflicts] : null
			} : null,
			Deduplication = hasDedup ? new MergeReportDeduplication
			{
				SecuritySchemes = [.. _deduplicatedSchemes]
			} : null,
			Removals = hasRemovals ? new MergeReportRemovals
			{
				ExcludedPaths = _excludedPaths.Count > 0 ? [.. _excludedPaths] : null,
				SkippedPaths = _skippedPaths.Count > 0 ? [.. _skippedPaths] : null
			} : null
		};
	}

	private static int? NullIfZero(int value) => value == 0 ? null : value;
}
