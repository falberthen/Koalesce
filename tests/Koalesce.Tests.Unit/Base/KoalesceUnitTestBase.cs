using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Koalesce.Tests.Unit;

/// <summary>
/// Base class for Koalesce unit tests.
/// </summary>
[Collection("Koalesce Unit Tests")]
public abstract class KoalesceUnitTestBase
{
	protected readonly IServiceCollection Services = new ServiceCollection();

	protected HttpContext CreateHttpContext(string requestPath)
	{
		var context = new DefaultHttpContext();
		context.Request.Path = requestPath;
		context.Response.Body = new MemoryStream(); // Required for response writing
		return context;
	}

	protected ILogger<T> CreateLogger<T>()
	{
		using var loggerFactory = LoggerFactory.Create(builder => builder.AddDebug());
		return loggerFactory.CreateLogger<T>();
	}
}