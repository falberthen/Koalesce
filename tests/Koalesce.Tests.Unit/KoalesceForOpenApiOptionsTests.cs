using Koalesce.OpenAPI.Options;

namespace Koalesce.Tests.Unit;

[Collection("Koalesce ForOpenAPI Unit Tests")]
public class KoalesceForOpenApiOptionsTests : KoalesceUnitTestBase
{
	[Fact]
	public void KoalesceForOpenAPI_WithGatewayUrlAndNoSecurity_ShouldNotThrow()
	{
		// Arrange
		var appSettingsStub = new
		{
			Koalesce = new
			{
				MergedDocumentPath = "/v1/mergedapidefinition.json",
				Sources = new[]
				{
					new { Url = "https://api1.com/v1/apidefinition.json" }
				},
				ApiGatewayBaseUrl = "http://localhost:5000"
				// OpenApiSecurityScheme is optional - allows mixed/public scenarios
			}
		};

		var configuration = ConfigurationHelper
			.BuildConfigurationFromObject(appSettingsStub);

		Services.AddKoalesce(configuration)
			.ForOpenAPI();

		var provider = Services.BuildServiceProvider();

		// Act & Assert - Should NOT throw
		var options = provider.GetRequiredService<IOptions<KoalesceOpenApiOptions>>().Value;

		Assert.NotNull(options);
		Assert.Equal("http://localhost:5000", options.ApiGatewayBaseUrl);
		Assert.Null(options.OpenApiSecurityScheme);
	}

	[Fact]
	public void KoalesceForOpenAPI_WithInvalidGatewayUrl_ShouldThrowValidationException()
	{
		// Arrange
		var appSettingsStub = new
		{
			Koalesce = new
			{
				MergedDocumentPath = "/v1/mergedapidefinition.json",
				Sources = new[]
				{
					new { Url = "https://api1.com/v1/apidefinition.json" }
				},
				ApiGatewayBaseUrl = "localhost:5000" // Invalid URL (no scheme http/https)
			}
		};

		var configuration = ConfigurationHelper
			.BuildConfigurationFromObject(appSettingsStub);

		Services.AddKoalesce(configuration)
			.ForOpenAPI();

		var provider = Services.BuildServiceProvider();

		// Act & Assert
		var exception = Assert.Throws<OptionsValidationException>(() =>
		{
			var options = provider.GetRequiredService<IOptions<KoalesceOpenApiOptions>>().Value;
		});

		Assert.Contains(OpenAPIConstants.ApiGatewayBaseUrlValidationError, exception.Message);
	}

	[Fact]
	public void KoalesceForOpenAPI_WithOpenApiSecurityScheme_ShouldBindCorrectly()
	{
		// Arrange
		var appSettingsStub = new
		{
			Koalesce = new
			{
				MergedDocumentPath = "/v1/mergedapidefinition.json",
				Sources = new[]
				{
					new { Url = "https://api1.com/v1/apidefinition.json" }
				},
				ApiGatewayBaseUrl = "http://localhost:5000",
				OpenApiSecurityScheme = new
				{
					Type = SecuritySchemeType.Http.ToString(),
					Scheme = "bearer",
					BearerFormat = "JWT",
					Description = "JWT Authorization"
				}
			}
		};

		var configuration = ConfigurationHelper
			.BuildConfigurationFromObject(appSettingsStub);

		Services.AddKoalesce(configuration)
			.ForOpenAPI();

		var provider = Services.BuildServiceProvider();

		// Act
		var options = provider.GetRequiredService<IOptions<KoalesceOpenApiOptions>>().Value;

		// Assert
		Assert.NotNull(options);
		Assert.Equal("http://localhost:5000", options.ApiGatewayBaseUrl);
		Assert.NotNull(options.OpenApiSecurityScheme);
		Assert.Equal(SecuritySchemeType.Http, options.OpenApiSecurityScheme.Type);
		Assert.Equal("bearer", options.OpenApiSecurityScheme.Scheme);
		Assert.Equal("JWT", options.OpenApiSecurityScheme.BearerFormat);
	}

	[Fact]
	public void KoalesceForOpenAPI_WithEmptyGatewayUrl_ShouldNotThrowValidationException()
	{
		// Arrange - Empty string is treated as Aggregation Mode
		var appSettingsStub = new
		{
			Koalesce = new
			{
				MergedDocumentPath = "/v1/mergedapidefinition.json",
				Sources = new[]
				{
					new { Url = "https://api1.com/v1/apidefinition.json" }
				},
				ApiGatewayBaseUrl = "" // Empty = Aggregation Mode
			}
		};

		var configuration = ConfigurationHelper
			.BuildConfigurationFromObject(appSettingsStub);

		Services.AddKoalesce(configuration)
			.ForOpenAPI();

		var provider = Services.BuildServiceProvider();

		// Act & Assert - Should NOT throw
		var options = provider.GetRequiredService<IOptions<KoalesceOpenApiOptions>>().Value;

		Assert.NotNull(options);
		Assert.Empty(options.ApiGatewayBaseUrl);
	}

	[Fact]
	public void KoalesceForOpenAPI_WithDefaultSchemaConflictPattern_ShouldUseDefault()
	{
		// Arrange
		var appSettingsStub = new
		{
			Koalesce = new
			{
				MergedDocumentPath = "/v1/mergedapidefinition.json",
				Sources = new[]
				{
					new { Url = "https://api1.com/v1/apidefinition.json" }
				}
			}
		};

		var configuration = ConfigurationHelper
			.BuildConfigurationFromObject(appSettingsStub);

		Services.AddKoalesce(configuration)
			.ForOpenAPI();

		var provider = Services.BuildServiceProvider();

		// Act
		var options = provider.GetRequiredService<IOptions<KoalesceOpenApiOptions>>().Value;

		// Assert - Default pattern
		Assert.Equal("{Prefix}_{SchemaName}", options.SchemaConflictPattern);
	}

	[Fact]
	public void KoalesceForOpenAPI_WithCustomSchemaConflictPattern_ShouldBindCorrectly()
	{
		// Arrange
		var appSettingsStub = new
		{
			Koalesce = new
			{
				MergedDocumentPath = "/v1/mergedapidefinition.json",
				Sources = new[]
				{
					new { Url = "https://api1.com/v1/apidefinition.json" }
				},
				SchemaConflictPattern = "{SchemaName}_{Prefix}"
			}
		};

		var configuration = ConfigurationHelper
			.BuildConfigurationFromObject(appSettingsStub);

		Services.AddKoalesce(configuration)
			.ForOpenAPI();

		var provider = Services.BuildServiceProvider();

		// Act
		var options = provider.GetRequiredService<IOptions<KoalesceOpenApiOptions>>().Value;

		// Assert - Custom pattern
		Assert.Equal("{SchemaName}_{Prefix}", options.SchemaConflictPattern);
	}

	[Theory]
	[InlineData("{Prefix}_Schema")]         // Missing {SchemaName}
	[InlineData("Schema_{SchemaName}")]     // Missing {Prefix}
	[InlineData("InvalidPattern")]          // Missing both placeholders
	[InlineData("{prefix}_{schemaname}")]   // Case-sensitive: wrong case
	public void KoalesceForOpenAPI_WithInvalidSchemaConflictPattern_ShouldThrowValidationException(string invalidPattern)
	{
		// Arrange
		var appSettingsStub = new
		{
			Koalesce = new
			{
				MergedDocumentPath = "/v1/mergedapidefinition.json",
				Sources = new[]
				{
					new { Url = "https://api1.com/v1/apidefinition.json" }
				},
				SchemaConflictPattern = invalidPattern
			}
		};

		var configuration = ConfigurationHelper
			.BuildConfigurationFromObject(appSettingsStub);

		Services.AddKoalesce(configuration)
			.ForOpenAPI();

		var provider = Services.BuildServiceProvider();

		// Act & Assert
		var exception = Assert.Throws<OptionsValidationException>(() =>
		{
			var options = provider.GetRequiredService<IOptions<KoalesceOpenApiOptions>>().Value;
		});

		Assert.Contains(OpenAPIConstants.SchemaConflictPatternValidationError, exception.Message);
	}
}