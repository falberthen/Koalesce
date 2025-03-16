using Koalesce.Core.Extensions;

namespace Koalesce.Core.Internals;

/// <summary>
/// Internal builder for Koalesce services and middleware.
/// </summary>
internal sealed class KoalesceBuilder : IKoalesceBuilder
{
	public IServiceCollection Services { get; }
	public IConfiguration Configuration { get; }

	// Track if middleware is enabled
	public bool UseMiddlewareEnabled { get; private set; } = false;

	internal KoalesceBuilder(IServiceCollection services, IConfiguration configuration)
	{
		ArgumentNullException.ThrowIfNull(services);
		ArgumentNullException.ThrowIfNull(configuration);
		Services = services;
		Configuration = configuration;
	}

	/// <summary>
	/// Public method to register a compatible custom provider and configure options
	/// </summary>	
	public IKoalesceBuilder AddProvider<TProvider, TOptions>()
		where TProvider : class, IKoalesceProvider
		where TOptions : KoalesceOptions, new()
	{
		this.RegisterKoalesceProvider<TProvider, TOptions>();
		return this;
	}

	/// <summary>
	/// Enable middleware registration
	/// </summary>
	internal void EnableMiddleware()
	{
		UseMiddlewareEnabled = true;
	}
}