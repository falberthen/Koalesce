namespace Koalesce.Extensions;

/// <summary>
/// Extension methods for configuring Koalesce
/// </summary>
public static class KoalesceExtensions
{
	/// <summary>
	/// Adds Koalesce services for OpenAPI specification merging.
	/// </summary>
	/// <param name="services">The service collection.</param>
	/// <param name="configuration">The configuration containing the Koalesce section.</param>
	/// <param name="configureOptions">Optional delegate to configure options programmatically.</param>
	public static IServiceCollection AddKoalesce(
		this IServiceCollection services,
		IConfiguration configuration,
		Action<KoalesceOptions>? configureOptions = null)
	{
		ArgumentNullException.ThrowIfNull(services);
		ArgumentNullException.ThrowIfNull(configuration);

		// Ensure Koalesce section exists
		var koalesceSection = configuration.GetSection(CoreOptions.ConfigurationSectionName);
		if (!koalesceSection.Exists())
			throw new KoalesceConfigurationNotFoundException();

		// Register options with validation
		services.AddOptions<KoalesceOptions>()
			.Bind(koalesceSection)
			.ValidateDataAnnotations()
			.ValidateOnStart();

		// Also bind base options for middleware
		services.AddOptions<CoreOptions>()
			.Bind(koalesceSection)
			.ValidateDataAnnotations()
			.PostConfigure(options => options.Validate());

		// Apply code-based configuration if provided
		if (configureOptions != null)
		{
			services.PostConfigure(configureOptions);
		}

		// Core services
		services.AddMemoryCache();

		// Merge services
		services.TryAddSingleton<OpenApiDocumentMerger>();
		services.TryAddSingleton<OpenApiDocumentSerializer>();
		services.TryAddSingleton<IKoalesceMergeService, OpenApiMergeService>();

		// Supporting services for merging
		services.TryAddSingleton<OpenApiDefinitionLoader>();
		services.TryAddSingleton<OpenApiPathMerger>();

		// Conflict resolution services
		services.TryAddSingleton<IConflictResolutionStrategy, DefaultConflictResolutionStrategy>();
		services.TryAddSingleton<SchemaRenamer>();
		services.TryAddSingleton<SchemaConflictCoordinator>();

		// Configure HttpClient for fetching API specs
		var httpTimeout = koalesceSection.GetValue<int?>(nameof(CoreOptions.HttpTimeoutSeconds))
			?? CoreConstants.DefaultHttpTimeoutSeconds;

		services.AddHttpClient(CoreConstants.KoalesceClient, client =>
		{
			client.DefaultRequestVersion = HttpVersion.Version11;
			client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
			client.Timeout = TimeSpan.FromSeconds(httpTimeout);
		})
		.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
		{
			SslOptions = new System.Net.Security.SslClientAuthenticationOptions
			{
				RemoteCertificateValidationCallback = delegate { return true; }
			},
			AutomaticDecompression = DecompressionMethods.All
		});

		return services;
	}

	/// <summary>
	/// Uses Koalesce middleware in the request pipeline.
	/// </summary>
	public static IApplicationBuilder UseKoalesce(this IApplicationBuilder app)
	{
		app.UseMiddleware<KoalesceMiddleware>();
		return app;
	}
}