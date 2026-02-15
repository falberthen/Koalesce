namespace Koalesce.Services;

/// <summary>
/// OpenAPI implementation of the Koalesce merge service.
/// </summary>
internal class OpenApiMergeService : IKoalesceMergeService
{
	private readonly ILogger<OpenApiMergeService> _logger;
	private readonly KoalesceOptions _options;
	private readonly OpenApiDocumentMerger _documentMerger;
	private readonly OpenApiDocumentSerializer _documentSerializer;

	public OpenApiMergeService(
		ILogger<OpenApiMergeService> logger,
		IOptions<KoalesceOptions> options,
		OpenApiDocumentMerger documentMerger,
		OpenApiDocumentSerializer documentSerializer)
	{
		ArgumentNullException.ThrowIfNull(logger);
		ArgumentNullException.ThrowIfNull(options);
		ArgumentNullException.ThrowIfNull(documentMerger);
		ArgumentNullException.ThrowIfNull(documentSerializer);

		_logger = logger;
		_options = options.Value;
		_documentMerger = documentMerger;
		_documentSerializer = documentSerializer;
	}

	/// <inheritdoc/>
	public async Task<MergeResult> MergeSpecificationsAsync(string? outputPath = null)
	{
		_logger.LogInformation("Starting Koalescing process");

		var (mergedDocument, sourceResults, report) = await _documentMerger.MergeIntoSingleSpecificationAsync();

		_logger.LogInformation("Koalescing complete. Loaded {LoadedCount}/{TotalCount} sources.",
			sourceResults.Count(r => r.IsLoaded), sourceResults.Count);

		var serializedDocument = await _documentSerializer
			.SerializeAsync(mergedDocument, outputPath);

		return new MergeResult(serializedDocument, sourceResults, report);
	}
}
