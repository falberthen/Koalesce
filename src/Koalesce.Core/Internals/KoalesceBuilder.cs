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
		// Registers the provider
		Services.TryAddSingleton<TProvider>();
		Services.TryAddSingleton<IKoalesceProvider>(sp => sp.GetRequiredService<TProvider>());

		// Registers options with active validation		
		Services.AddOptions<TOptions>()
			.Bind(Configuration.GetSection(KoalesceOptions.ConfigurationSectionName))
			.ValidateDataAnnotations() // Validate attributes like [Required]
			.ValidateOnStart();        // Forces validation at app startup

		// Configure a named HttpClient for Koalesce
		var koalesceSection = Configuration.GetSection(KoalesceOptions.ConfigurationSectionName);
		var httpTimeout = koalesceSection.GetValue<int?>(nameof(KoalesceOptions.HttpTimeoutSeconds))
			?? CoreConstants.DefaultHttpTimeoutSeconds;

		Services.AddHttpClient(CoreConstants.KoalesceClient, client =>
		{
			client.DefaultRequestVersion = HttpVersion.Version11;
			client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
			client.Timeout = TimeSpan.FromSeconds(httpTimeout);
		})
		.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
		{
			SslOptions = new System.Net.Security.SslClientAuthenticationOptions
			{
				// Allow untrusted/self-signed certificates (common in dev/localhost)
				RemoteCertificateValidationCallback = delegate { return true; }
			},
			AutomaticDecompression = DecompressionMethods.All
		});

		// Enable middleware registration
		EnableMiddleware();

		return this;
	}

	/// <summary>
	/// Enable middleware registration
	/// </summary>
	private void EnableMiddleware()
	{
		UseMiddlewareEnabled = true;
	}
}