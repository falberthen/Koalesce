namespace Koalesce.Core.Tests.DummyProviders;

// Dummy provider for testing
internal class DummyProvider : IKoalesceProvider
{
	public int CallCount { get; private set; } = 0;

	public Task<string> ProvideMergedDocumentAsync()
	{
		CallCount++;
		// Returns a JSON that changes each call for proving result was cached (or not)
		return Task.FromResult($"{{\"openapi\": \"3.0.1\", \"x-generated-id\": {CallCount}}}");
	}
}