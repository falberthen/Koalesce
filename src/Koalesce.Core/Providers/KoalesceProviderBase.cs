namespace Koalesce.Core.Providers;

/// <summary>
/// Base class for all Koalesce providers to enforce a structured merging process.
/// </summary>
public abstract class KoalesceProviderBase<TOptions, TMergeResult> : IKoalesceProvider
	where TOptions : KoalesceOptions
	where TMergeResult : class
{
	private readonly ILogger<KoalesceProviderBase<TOptions, TMergeResult>> Logger;
	private readonly TOptions Options;
	private readonly IDocumentMerger<TMergeResult> DocumentMerger;
	private readonly IMergedDocumentSerializer<TMergeResult> MergedDocumentSerializer;

	protected KoalesceProviderBase(
		ILogger<KoalesceProviderBase<TOptions, TMergeResult>> logger,
		IOptions<TOptions> options,
		IDocumentMerger<TMergeResult> documentMerger,
		IMergedDocumentSerializer<TMergeResult> mergedDocumentSerializer)
	{
		ArgumentNullException.ThrowIfNull(logger);
		ArgumentNullException.ThrowIfNull(options);
		ArgumentNullException.ThrowIfNull(documentMerger);
		ArgumentNullException.ThrowIfNull(mergedDocumentSerializer);

		Logger = logger;
		Options = options.Value;
		DocumentMerger = documentMerger;
		MergedDocumentSerializer = mergedDocumentSerializer;
	}

	/// <inheritdoc/>
	public async Task<string> ProvideMergedDocumentAsync()
	{
		string providerName = GetType().Name;

		Logger.LogInformation("Starting Koalesce merge process for {Provider}", providerName);

		TMergeResult mergedDocument = await DocumentMerger
			.MergeIntoSingleDefinitionAsync();

		Logger.LogInformation("Koalescing complete using {Provider} for merging {SourcesCount} definitions.",
			providerName, Options.Sources.Count);

		return MergedDocumentSerializer
			.Serialize(mergedDocument);
	}
}
