using Koalesce.Core;
using Koalesce.Core.Exceptions;
using Koalesce.Tests.Unit.DummyProviders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Koalesce.Tests.Unit;

public class KoalesceCoreTests : KoalesceUnitTestBase
{
	private readonly IServiceCollection _services;

	public KoalesceCoreTests()
	{
		_services = new ServiceCollection();
	}

	[Fact]
	public void AddKoalesce_WhenServicesAndConfigurationProvided_ShouldRegisterDependencies()
	{
		// Arrange
		IConfiguration configuration = LoadConfigurations("appsettings.json");

		// Act
		_services.AddKoalesce(configuration)
			.AddProvider<DummyProvider, KoalesceOptions>();

		var provider = _services.BuildServiceProvider();

		// Assert
		Assert.NotNull(provider.GetService<IKoalesceBuilder>());
		Assert.NotNull(provider.GetService<IOptions<KoalesceOptions>>());
	}

	[Fact]
	public void AddKoalesce_ShouldRegisterSingletonKoalesceBuilder()
	{
		// Arrange
		IConfiguration configuration = LoadConfigurations("appsettings.json");

		_services.AddKoalesce(configuration);
		var provider = _services.BuildServiceProvider();

		// Act
		var builder1 = provider.GetService<IKoalesceBuilder>();
		var builder2 = provider.GetService<IKoalesceBuilder>();

		// Assert
		Assert.NotNull(builder1);
		Assert.Same(builder1, builder2); // Singleton instance
	}

	[Fact]
	public void AddKoalesce_WhenNonRequiredConfigValuesAreMissing_ShouldUseDefaultValues()
	{
		// Arrange		
		IConfiguration configuration = LoadConfigurations("appsettings.only-required-values.json");

		_services.AddKoalesce(configuration)
			.AddProvider<DummyProvider, KoalesceOptions>();

		var provider = _services.BuildServiceProvider();

		// Act
		var options = provider.GetService<IOptions<KoalesceOptions>>()?.Value;

		// Assert
		Assert.NotNull(options);
		Assert.Equal(KoalesceOptions.TitleDefaultValue, options.Title);		
	}

	[Fact]
	public void AddKoalesce_WhenValidConfiguration_ShouldBindKoalesceOptions()
	{
		// Arrange
		IConfiguration configuration = LoadConfigurations("appsettings.json");

		_services.AddKoalesce(configuration)
			.AddProvider<DummyProvider, KoalesceOptions>();

		var provider = _services.BuildServiceProvider();

		var expectedRoutes = new List<string>()
		{
			"https://localhost:5001/swagger/v1/swagger.json",
			"https://localhost:5002/swagger/v1/swagger.json"
		};

		// Act
		var options = provider.GetService<IOptions<KoalesceOptions>>()?.Value;

		// Assert
		Assert.NotNull(options);
		Assert.Equal("My Koalesced API", options.Title);		
		Assert.Equal("/swagger/v1/apigateway.yaml", options.MergedOpenApiPath);
		Assert.Equal(expectedRoutes, options.SourceOpenApiUrls);
	}

	[Fact]
	public void UseKoalesce_WhenAddProvider_ShouldEnableMiddleware()
	{
		// Arrange
		IConfiguration configuration = LoadConfigurations("appsettings.json");
		var builder = _services.AddKoalesce(configuration)
			.AddProvider<DummyProvider, KoalesceOptions>();

		var provider = builder.Services.BuildServiceProvider();
		var app = new FakeApplicationBuilder(provider);

		// Act
		app.UseKoalesce();

		// Assert
		Assert.True(app.MiddlewareRegistered);
	}

	[Fact]
	public void AddKoalesce_WhenConfigurationIsNull_ShouldThrowArgumentNullException()
	{
		// Act & Assert
		var exception = Assert.Throws<ArgumentNullException>(() =>
			_services.AddKoalesce(null!));

		Assert.Equal("configuration", exception.ParamName);
	}

	[Fact]
	public void AddKoalesce_WhenServicesIsNull_ShouldThrowArgumentNullException()
	{
		// Arrange
		IConfiguration configuration = LoadConfigurations("appsettings.json");
		IServiceCollection? nullServices = null;

		// Act & Assert
		var exception = Assert.Throws<ArgumentNullException>(() =>
			nullServices!.AddKoalesce(configuration));

		Assert.Equal("services", exception.ParamName);
	}

	[Fact]
	public void AddKoalesce_WhenKoalesceSectionIsMissing_ShouldThrowKoalesceConfigurationNotFoundException()
	{
		// Arrange: Create an empty configuration (no "Koalesce" section)
		IConfiguration missingConfig = LoadConfigurations("appsettings-missing-koalesce.json");

		// Act & Assert: Expect custom exception when attempting to register Koalesce
		Assert.Throws<KoalesceConfigurationNotFoundException>(() =>
			_services.AddKoalesce(missingConfig));
	}

	[Fact]
	public void AddProvider_WhenKoalesceSectionIsMissing_ShouldThrowKoalesceConfigurationNotFoundException()
	{
		// Arrange
		IConfiguration emptyConfiguration = LoadConfigurations("appsettings-missing-koalesce.json");

		// Act & Assert: Expect custom exception when attempting to use Koalesce
		Assert.Throws<KoalesceConfigurationNotFoundException>(() =>
			_services.AddKoalesce(emptyConfiguration)
				.AddProvider<DummyProvider, KoalesceOptions>());
	}

	[Fact]
	public void AddProvider_WhenKoalesceRequiredConfigurationMissing_ShouldThrowKoalesceRequiredConfigurationValuesNotFoundException()
	{
		// Arrange
		IConfiguration emptyConfiguration = LoadConfigurations("appsettings-missing-required-values.json");

		// Act & Assert: Expect custom exception when attempting to use Koalesce
		Assert.Throws<KoalesceRequiredConfigurationValuesNotFoundException>(() =>
			_services.AddKoalesce(emptyConfiguration)
				.AddProvider<DummyProvider, KoalesceOptions>());
	}
}