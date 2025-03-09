namespace Koalesce.Core;

/// <summary>
/// Defines the contract for building Koalesce services and middleware.
/// </summary>
public interface IKoalesceBuilder
{
	/// <summary>
	/// Add a custom Koalesce provider.
	/// </summary>
	IKoalesceBuilder AddProvider<TProvider, TOptions>()
		where TProvider : class, IKoalesceProvider
		where TOptions : KoalesceOptions, new();

	/// <summary>
	/// IServiceCollection to allow service registrations.
	/// </summary>
	IServiceCollection Services { get; }

	/// <summary>
	/// IConfiguration for accessing settings.
	/// </summary>
	IConfiguration Configuration { get; }
}
