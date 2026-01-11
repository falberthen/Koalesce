namespace Koalesce.Tests.Unit;

[Collection("Koalesce Core Extension Unit Tests")]
public class KoalesceCoreExtensionsTests : KoalesceUnitTestBase
{
	[Fact]
	public void AddKoalesce_WhenServicesAndConfigurationProvided_ShouldRegisterDependencies()
	{
		// Arrange
		var configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				{ "Koalesce:MergedOpenApiPath", "/swagger/v1/swagger.json" },
				{ "Koalesce:SourceOpenApiUrls:0", "https://api1.com/swagger/v1/swagger.json" },
				{ "Koalesce:SourceOpenApiUrls:1", "https://api2.com/swagger/v1/swagger.json" }
			})
			.Build();

		// Act
		Services.AddKoalesce(configuration)
			.AddProvider<DummyProvider, KoalesceOptions>();

		var provider = Services.BuildServiceProvider();

		// Assert
		Assert.NotNull(provider.GetService<IKoalesceBuilder>());
		Assert.NotNull(provider.GetService<IOptions<KoalesceOptions>>());
	}

	[Fact]
	public void AddKoalesce_ShouldRegisterSingletonKoalesceBuilder()
	{
		// Arrange
		var configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				{ "Koalesce:MergedOpenApiPath", "/swagger/v1/swagger.json" },
				{ "Koalesce:SourceOpenApiUrls:0", "https://api1.com/swagger/v1/swagger.json" },
				{ "Koalesce:SourceOpenApiUrls:1", "https://api2.com/swagger/v1/swagger.json" }
			}).Build();

		Services.AddKoalesce(configuration)
			.AddProvider<DummyProvider, KoalesceOptions>();

		var provider = Services.BuildServiceProvider();

		// Act
		var builder1 = provider.GetService<IKoalesceBuilder>();
		var builder2 = provider.GetService<IKoalesceBuilder>();

		// Assert
		Assert.NotNull(builder1);
		Assert.Same(builder1, builder2); // Singleton instance
	}

	[Fact]
	public void UseKoalesce_WhenAddProvider_ShouldEnableMiddleware()
	{
		// Arrange
		var configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				{ "Koalesce:MergedOpenApiPath", "/swagger/v1/swagger.json" },
				{ "Koalesce:OpenApiSources:0:Url", "https://api1.com/swagger/v1/swagger.json" }
			})
			.Build();

		var builder = Services.AddKoalesce(configuration)
			.AddProvider<DummyProvider, DummyOptions>();

		var provider = builder.Services.BuildServiceProvider();
		var app = new FakeApplicationBuilder(provider);

		// Act
		app.UseKoalesce();

		// Assert
		Assert.True(app.MiddlewareRegistered);
	}

	[Fact]
	public void AddKoalesce_WhenServicesIsNull_ShouldThrowArgumentNullException()
	{
		// Arrange
		IServiceCollection? nullServices = null;
		var configuration = new ConfigurationBuilder().Build();

		// Act & Assert
		var exception = Assert.Throws<ArgumentNullException>(() =>
			nullServices!.AddKoalesce(configuration));

		Assert.Equal("services", exception.ParamName);
	}

	[Fact]
	public void AddKoalesce_WhenConfigurationIsNull_ShouldThrowArgumentNullException()
	{
		// Act & Assert
		var exception = Assert.Throws<ArgumentNullException>(() =>
			Services.AddKoalesce(null!));

		Assert.Equal("configuration", exception.ParamName);
	}
}