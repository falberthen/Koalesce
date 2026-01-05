namespace Koalesce.Core.Extensions;

/// <summary>
/// Extension methods for configuring Koalesce.
/// </summary>
public static class KoalesceExtensions
{
	/// <summary>
	/// Adds Koalesce core services and returns an IKoalesceBuilder for extensions.
	/// </summary>
	public static IKoalesceBuilder AddKoalesce(this IServiceCollection services, IConfiguration configuration)
	{
		ArgumentNullException.ThrowIfNull(services);
		ArgumentNullException.ThrowIfNull(configuration);

		var builder = new KoalesceBuilder(services, configuration);

		// Ensure Koalesce section exists
		var koalesceSection = configuration.GetSection(KoalesceOptions.ConfigurationSectionName);
		if (!koalesceSection.Exists())
			throw new KoalesceConfigurationNotFoundException();

		services
			.AddOptions<KoalesceOptions>()
			.Bind(koalesceSection)
			.ValidateDataAnnotations()
			.PostConfigure(options =>
			{
				options.Validate();
			});

		services.AddSingleton<IKoalesceBuilder>(builder);
		services.AddMemoryCache();

		return builder;
	}

	/// <summary>
	/// Uses Koalesce middleware in the request pipeline IF enabled in KoalesceBuilder
	/// </summary>
	public static IApplicationBuilder UseKoalesce(this IApplicationBuilder app)
	{
		if (app.ApplicationServices.GetRequiredService<IKoalesceBuilder>() is KoalesceBuilder koalesceBuilder
			&& koalesceBuilder.UseMiddlewareEnabled)
		{
			app.UseMiddleware<KoalesceMiddleware>();
		}

		return app;
	}

	/// <summary>
	/// Registers a Koalesce provider with specific options, ensuring no duplicate bindings.
	/// </summary>
	internal static IKoalesceBuilder RegisterKoalesceProvider<TProvider, TOptions>(this IKoalesceBuilder builder)
		where TProvider : class, IKoalesceProvider
		where TOptions : KoalesceOptions, new()
	{
		var services = builder.Services;

		// Ensuring the provider is only added once
		services.TryAddSingleton<IKoalesceProvider, TProvider>();

		var koalesceSection = builder.Configuration
			.GetSection(KoalesceOptions.ConfigurationSectionName);

		// Binding provider-specific options
		if (typeof(TOptions) != typeof(KoalesceOptions))
		{
			services.Configure<TOptions>(koalesceSection);
		}

		// Configuring HttpClient for Koalesce
		services.AddHttpClient(CoreConstants.KoalesceClient, client =>
		{
			client.DefaultRequestVersion = HttpVersion.Version11;
			client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
			client.Timeout = TimeSpan.FromSeconds(15);
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

		// Enabling Middleware
		if (builder is KoalesceBuilder koalesceBuilder)
			koalesceBuilder.EnableMiddleware();

		return builder;
	}
}