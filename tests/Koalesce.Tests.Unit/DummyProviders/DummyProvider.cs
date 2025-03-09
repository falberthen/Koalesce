using Koalesce.Core;

namespace Koalesce.Tests.Unit.DummyProviders;

// Dummy provider for testing
internal class DummyProvider : IKoalesceProvider
{
	public Task<string> ProvideSerializedDocumentAsync() => Task.FromResult("{}");
}