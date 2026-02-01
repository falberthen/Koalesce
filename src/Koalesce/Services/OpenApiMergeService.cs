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
	public async Task<string> MergeDefinitionsAsync(string? outputPath = null)
	{
		_logger.LogInformation("Starting Koalesce merge process");

		var mergedDocument = await _documentMerger.MergeIntoSingleDefinitionAsync();

		_logger.LogInformation("Koalescing complete for merging {SourcesCount} definitions.",
			_options.Sources.Count);

		return _documentSerializer.Serialize(mergedDocument, outputPath);
	}
}
