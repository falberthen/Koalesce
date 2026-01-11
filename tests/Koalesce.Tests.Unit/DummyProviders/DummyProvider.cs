namespace Koalesce.Tests.Unit.DummyProviders;

// Dummy provider for testing
internal class DummyProvider : IKoalesceProvider
{
	public Task<string> ProvideMergedDocumentAsync() => 
		Task.FromResult("{}");
}

internal class DummyOptions : KoalesceOptions
{
}