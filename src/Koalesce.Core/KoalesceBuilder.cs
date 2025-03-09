namespace Koalesce.Core;

/// <summary>
/// Builder for Koalesce services and middleware.
/// </summary>
public class KoalesceBuilder : IKoalesceBuilder
{
	public IServiceCollection Services { get; }
	public IConfiguration Configuration { get; }

	// Track if middleware is enabled
	public bool UseMiddlewareEnabled { get; private set; } = false;

	public KoalesceBuilder(IServiceCollection services, IConfiguration configuration)
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