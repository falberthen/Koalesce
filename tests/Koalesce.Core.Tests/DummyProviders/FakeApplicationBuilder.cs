namespace Koalesce.Core.Tests.DummyProviders;

// Fake middleware application builder
internal class FakeApplicationBuilder : IApplicationBuilder
{
	public IServiceProvider ApplicationServices { get; set; }
	public bool MiddlewareRegistered { get; private set; } = false;

	public FakeApplicationBuilder(IServiceProvider provider)
	{
		ApplicationServices = provider;
	}

	public IApplicationBuilder Use(Func<RequestDelegate, RequestDelegate> middleware)
	{
		MiddlewareRegistered = true;
		return this;
	}

	// Other IApplicationBuilder members (not used in the test)
	public IDictionary<string, object?> Properties => 
		throw new NotImplementedException();
	public IFeatureCollection ServerFeatures => 
		throw new NotImplementedException();
	public RequestDelegate? Build() => 
		throw new NotImplementedException();
	public IApplicationBuilder New() => 
		throw new NotImplementedException();
}
