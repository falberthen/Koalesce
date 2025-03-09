namespace Koalesce.Core;

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

		// Checking if KoalesceOptions is already bound
		if (!services.Any(s => s.ServiceType == typeof(IOptions<KoalesceOptions>)))
		{
			// then bind it
			services.Configure<KoalesceOptions>(koalesceSection);
		}

		// Validating configuration before service registration
		using var provider = services.BuildServiceProvider();
		var options = provider.GetRequiredService<IOptions<KoalesceOptions>>().Value;
		ValidateKoalesceOptions(options);

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
		if (!services.Any(s => s.ServiceType == typeof(IOptions<TOptions>)))
		{
			services.Configure<TOptions>(koalesceSection);
		}

		// Using PostConfigure to avoid overriding existing values
		services.PostConfigure<TOptions>(options =>
		{
			options.SourceOpenApiUrls = options.SourceOpenApiUrls
				.Distinct().ToList();
		});

		services.AddHttpClient();

		// Enabling Middleware
		if (builder is KoalesceBuilder koalesceBuilder)
			koalesceBuilder.EnableMiddleware();

		return builder;
	}

	/// <summary>
	/// Validation method for required configuration fields
	/// </summary>
	/// <param name="options"></param>
	/// <exception cref="KoalesceRequiredConfigurationValuesNotFoundException"></exception>
	private static void ValidateKoalesceOptions(KoalesceOptions options)
	{
		var validationResults = new List<ValidationResult>();
		var context = new ValidationContext(options);
		bool isValid = Validator.TryValidateObject(options, context, validationResults, true);

		if (!isValid)
		{
			var errorMessages = string.Join("; ", validationResults.Select(v => v.ErrorMessage));
			throw new KoalesceRequiredConfigurationValuesNotFoundException(
				$"Koalesce configuration is invalid: {errorMessages}"
			);
		}
	}
}