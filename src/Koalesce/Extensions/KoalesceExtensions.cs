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
	/// <param name="configureHttpClient">Optional delegate to configure the HttpClient used for fetching API specs.
	/// Use this to customize SSL/TLS settings, add handlers, or configure other HTTP behaviors.</param>
	public static IServiceCollection AddKoalesce(
		this IServiceCollection services,
		IConfiguration configuration,
		Action<KoalesceOptions>? configureOptions = null,
		Action<IHttpClientBuilder>? configureHttpClient = null)
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

		// Apply code-based configuration if provided
		if (configureOptions != null)
			services.PostConfigure(configureOptions);

		// Derive CoreOptions (used by middleware) from the fully-configured KoalesceOptions.
		// This ensures programmatic overrides via configureOptions are visible to the middleware.
		services.AddSingleton<IOptions<CoreOptions>>(sp =>
		{
			CoreOptions opts = sp.GetRequiredService<IOptions<KoalesceOptions>>().Value;
			return Microsoft.Extensions.Options.Options.Create(opts);
		});

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
		services.TryAddSingleton<ISchemaReferenceWalker, SchemaReferenceWalker>();
		services.TryAddSingleton<ISchemaRenamer, SchemaRenamer>();
		services.TryAddSingleton<SchemaConflictCoordinator>();
		services.TryAddSingleton<SecuritySchemeConflictCoordinator>();

		// Configure HttpClient for fetching API specs
		var httpTimeout = koalesceSection.GetValue<int?>(nameof(CoreOptions.HttpTimeoutSeconds))
			?? CoreConstants.DefaultHttpTimeoutSeconds;

		var httpClientBuilder = services.AddHttpClient(CoreConstants.KoalesceClient, client =>
		{
			client.DefaultRequestVersion = HttpVersion.Version11;
			client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
			client.Timeout = TimeSpan.FromSeconds(httpTimeout);
		});

		// Allow consumer to customize HttpClient (e.g., SSL/TLS, handlers, etc.)
		configureHttpClient?.Invoke(httpClientBuilder);

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