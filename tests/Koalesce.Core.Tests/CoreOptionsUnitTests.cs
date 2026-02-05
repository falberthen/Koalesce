namespace Koalesce.Core.Tests;

[Collection("Koalesce Core Options Unit Tests")]
public class CoreOptionsUnitTests : KoalesceUnitTestBase
{
	[Fact]
	public void Koalesce_WhenNonRequiredConfigValuesAreMissing_ShouldUseDefaultValues()
	{
		// Arrange
		var appSettingsStub = new
		{
			Koalesce = new CoreOptions
			{
				MergedEndpoint = "/v1/mergedapidefinition.json",
				Sources = new List<ApiSource>
				{
					new ApiSource { Url = "https://localhost:5001/v1/apidefinition.json" }
				}
			}
		};

		var configuration = ConfigurationHelper
			.BuildConfigurationFromObject(appSettingsStub);

		Services.AddKoalesce(configuration);

		var provider = Services.BuildServiceProvider();

		// Act
		var options = provider.GetService<IOptions<KoalesceOptions>>()?.Value;

		// Assert
		Assert.NotNull(options);
		Assert.Equal(CoreOptions.TitleDefaultValue, options.Title); // Default title should be set
	}

	[Fact]
	public void Koalesce_WhenValidConfiguration_ShouldBindKoalesceOptions()
	{
		// Arrange
		var appSettingsStub = new
		{
			Koalesce = new CoreOptions
			{
				Title = "My Koalesced API",
				MergedEndpoint = "/v1/mergedapidefinition.yaml",
				Sources = new List<ApiSource>
				{
					new ApiSource { Url = "https://localhost:5001/v1/apidefinition.json" },
					new ApiSource { Url = "https://localhost:5002/v1/apidefinition.json" }
				}
			}
		};

		var configuration = ConfigurationHelper
			.BuildConfigurationFromObject(appSettingsStub);

		Services.AddKoalesce(configuration);

		var provider = Services.BuildServiceProvider();

		var expectedRoutes = new List<ApiSource>()
		{
			new ApiSource { Url = "https://localhost:5001/v1/apidefinition.json" },
			new ApiSource { Url = "https://localhost:5002/v1/apidefinition.json" }
		};

		// Act
		var options = provider.GetService<IOptions<KoalesceOptions>>()?.Value;

		// Assert
		Assert.NotNull(options);
		Assert.Equal("My Koalesced API", options.Title);
		Assert.Equal("/v1/mergedapidefinition.yaml", options.MergedEndpoint);

		Assert.Equal(expectedRoutes.Count, options.Sources.Count);
		Assert.Equal(expectedRoutes[0].Url, options.Sources[0].Url);
		Assert.Equal(expectedRoutes[1].Url, options.Sources[1].Url);
	}

	[Fact]
	public void Koalesce_WhenKoalesceSectionIsMissing_ShouldThrowKoalesceConfigurationNotFoundException()
	{
		// Arrange: Empty configuration (simulating appsettings.json without "Koalesce" section)
		var appSettingsStub = new { OtherSection = "Irrelevant" };
		var configuration = ConfigurationHelper
			.BuildConfigurationFromObject(appSettingsStub);

		// Act & Assert: Expect custom exception when attempting to use Koalesce
		Assert.Throws<KoalesceConfigurationNotFoundException>(() =>
			Services.AddKoalesce(configuration));
	}

	[Fact]
	public void Koalesce_WhenOpenApiSourcesContainInvalidSourceUrl_ShouldThrowValidationException()
	{
		// Arrange
		var appSettingsStub = new
		{
			Koalesce = new CoreOptions
			{
				MergedEndpoint = "/v1/mergedapidefinition.json",
				Sources = new List<ApiSource>
				{
					new ApiSource { Url = "https://api1.com/v1/apidefinition.json" },
					new ApiSource { Url = "localhost:8002/v1/apidefinition.json" } // Invalid URL (missing scheme)
                }
			}
		};

		var configuration = ConfigurationHelper
			.BuildConfigurationFromObject(appSettingsStub);

		Services.AddKoalesce(configuration);

		var provider = Services.BuildServiceProvider();

		// Act & Assert
		var exception = Assert.Throws<OptionsValidationException>(() =>
		{
			var options = provider.GetRequiredService<IOptions<KoalesceOptions>>().Value;
		});

		Assert.Contains("must be a valid absolute URL", exception.Message);
		Assert.Contains("index 1", exception.Message);
	}

	#region SchemaConflictPattern Validation Tests

	[Fact]
	public void Koalesce_WithDefaultSchemaConflictPattern_ShouldUseDefault()
	{
		// Arrange
		var appSettingsStub = new
		{
			Koalesce = new
			{
				MergedEndpoint = "/v1/mergedapidefinition.json",
				Sources = new[]
				{
					new { Url = "https://api1.com/v1/apidefinition.json" }
				}
			}
		};

		var configuration = ConfigurationHelper
			.BuildConfigurationFromObject(appSettingsStub);

		Services.AddKoalesce(configuration);

		var provider = Services.BuildServiceProvider();

		// Act
		var options = provider.GetRequiredService<IOptions<KoalesceOptions>>().Value;

		// Assert - Default pattern
		Assert.Equal("{Prefix}{SchemaName}", options.SchemaConflictPattern);
	}

	[Fact]
	public void Koalesce_WithCustomSchemaConflictPattern_ShouldBindCorrectly()
	{
		// Arrange
		var appSettingsStub = new
		{
			Koalesce = new
			{
				MergedEndpoint = "/v1/mergedapidefinition.json",
				Sources = new[]
				{
					new { Url = "https://api1.com/v1/apidefinition.json" }
				},
				SchemaConflictPattern = "{SchemaName}_{Prefix}"
			}
		};

		var configuration = ConfigurationHelper
			.BuildConfigurationFromObject(appSettingsStub);

		Services.AddKoalesce(configuration);

		var provider = Services.BuildServiceProvider();

		// Act
		var options = provider.GetRequiredService<IOptions<KoalesceOptions>>().Value;

		// Assert - Custom pattern
		Assert.Equal("{SchemaName}_{Prefix}", options.SchemaConflictPattern);
	}

	[Theory]
	[InlineData("{Prefix}_Schema")]         // Missing {SchemaName}
	[InlineData("Schema_{SchemaName}")]     // Missing {Prefix}
	[InlineData("InvalidPattern")]          // Missing both placeholders
	[InlineData("{prefix}_{schemaname}")]   // Case-sensitive: wrong case
	public void Koalesce_WithInvalidSchemaConflictPattern_ShouldThrowValidationException(string invalidPattern)
	{
		// Arrange
		var appSettingsStub = new
		{
			Koalesce = new
			{
				MergedEndpoint = "/v1/mergedapidefinition.json",
				Sources = new[]
				{
					new { Url = "https://api1.com/v1/apidefinition.json" }
				},
				SchemaConflictPattern = invalidPattern
			}
		};

		var configuration = ConfigurationHelper
			.BuildConfigurationFromObject(appSettingsStub);

		Services.AddKoalesce(configuration);

		var provider = Services.BuildServiceProvider();

		// Act & Assert
		var exception = Assert.Throws<OptionsValidationException>(() =>
		{
			var options = provider.GetRequiredService<IOptions<KoalesceOptions>>().Value;
		});

		Assert.Contains(CoreConstants.SchemaConflictPatternValidationError, exception.Message);
	}

	#endregion

	#region ExcludePaths Validation Tests

	[Fact]
	public void Koalesce_WhenExcludePathsIsEmpty_ShouldThrowValidationException()
	{
		// Arrange
		var appSettingsStub = new
		{
			Koalesce = new CoreOptions
			{
				MergedEndpoint = "/v1/mergedapidefinition.json",
				Sources = new List<ApiSource>
			{
				new ApiSource
				{
					Url = "https://api1.com/v1/apidefinition.json",
					ExcludePaths = new List<string> { "" }
				}
			}
			}
		};

		var configuration = ConfigurationHelper
			.BuildConfigurationFromObject(appSettingsStub);

		Services.AddKoalesce(configuration);

		var provider = Services.BuildServiceProvider();

		// Act & Assert
		var exception = Assert.Throws<OptionsValidationException>(() =>
		{
			var options = provider.GetRequiredService<IOptions<KoalesceOptions>>().Value;
		});

		Assert.Contains("cannot be empty", exception.Message);
	}

	[Fact]
	public void Koalesce_WhenExcludePathsDoesNotStartWithSlash_ShouldThrowValidationException()
	{
		// Arrange
		var appSettingsStub = new
		{
			Koalesce = new CoreOptions
			{
				MergedEndpoint = "/v1/mergedapidefinition.json",
				Sources = new List<ApiSource>
			{
				new ApiSource
				{
					Url = "https://api1.com/v1/apidefinition.json",
					ExcludePaths = new List<string> { "api/admin" }
				}
			}
			}
		};

		var configuration = ConfigurationHelper
			.BuildConfigurationFromObject(appSettingsStub);

		Services.AddKoalesce(configuration);

		var provider = Services.BuildServiceProvider();

		// Act & Assert
		var exception = Assert.Throws<OptionsValidationException>(() =>
		{
			var options = provider.GetRequiredService<IOptions<KoalesceOptions>>().Value;
		});

		Assert.Contains("must start with '/'", exception.Message);
	}

	[Theory]
	[InlineData("/api/**/users")]
	[InlineData("/api/**")]
	[InlineData("/**/admin")]
	[InlineData("/api/a**b")]
	public void Koalesce_WhenExcludePathsHasInvalidWildcard_ShouldThrowValidationException(string invalidPath)
	{
		// Arrange
		var appSettingsStub = new
		{
			Koalesce = new CoreOptions
			{
				MergedEndpoint = "/v1/mergedapidefinition.json",
				Sources = new List<ApiSource>
			{
				new ApiSource
				{
					Url = "https://api1.com/v1/apidefinition.json",
					ExcludePaths = new List<string> { invalidPath }
				}
			}
			}
		};

		var configuration = ConfigurationHelper
			.BuildConfigurationFromObject(appSettingsStub);

		Services.AddKoalesce(configuration);

		var provider = Services.BuildServiceProvider();

		// Act & Assert
		var exception = Assert.Throws<OptionsValidationException>(() =>
		{
			var options = provider.GetRequiredService<IOptions<KoalesceOptions>>().Value;
		});

		Assert.Contains("invalid wildcard", exception.Message);
	}

	[Theory]
	[InlineData("/api/admin")]
	[InlineData("/api/admin/*")]
	[InlineData("/api/internal/health")]
	[InlineData("/api/*/users")]
	[InlineData("/*/admin")]
	[InlineData("/api/admin*")]
	[InlineData("/api/*/details")]
	[InlineData("/*suffix")]
	[InlineData("/prefix*")]
	public void Koalesce_WhenExcludePathsIsValid_ShouldNotThrowException(string validPath)
	{
		// Arrange
		var appSettingsStub = new
		{
			Koalesce = new CoreOptions
			{
				MergedEndpoint = "/v1/mergedapidefinition.json",
				Sources = new List<ApiSource>
			{
				new ApiSource
				{
					Url = "https://api1.com/v1/apidefinition.json",
					ExcludePaths = new List<string> { validPath }
				}
			}
			}
		};

		var configuration = ConfigurationHelper
			.BuildConfigurationFromObject(appSettingsStub);

		Services.AddKoalesce(configuration);

		var provider = Services.BuildServiceProvider();

		// Act & Assert - Should not throw
		var options = provider.GetRequiredService<IOptions<KoalesceOptions>>().Value;
		Assert.NotNull(options);
		Assert.Contains(validPath, options.Sources[0].ExcludePaths!);
	}

	#endregion

	#region VirtualPrefix Validation Tests

	[Fact]
	public void Koalesce_WhenDuplicateVirtualPrefix_ShouldThrowValidationException()
	{
		// Arrange
		var appSettingsStub = new
		{
			Koalesce = new CoreOptions
			{
				MergedEndpoint = "/v1/mergedapidefinition.json",
				Sources = new List<ApiSource>
				{
					new ApiSource
					{
						Url = "https://api1.com/v1/apidefinition.json",
						VirtualPrefix = "/inventory"
					},
					new ApiSource
					{
						Url = "https://api2.com/v1/apidefinition.json",
						VirtualPrefix = "/inventory"  // Duplicate!
					}
				}
			}
		};

		var configuration = ConfigurationHelper
			.BuildConfigurationFromObject(appSettingsStub);

		Services.AddKoalesce(configuration);

		var provider = Services.BuildServiceProvider();

		// Act & Assert
		var exception = Assert.Throws<OptionsValidationException>(() =>
		{
			var options = provider.GetRequiredService<IOptions<KoalesceOptions>>().Value;
		});

		Assert.Contains("Duplicate VirtualPrefix", exception.Message);
		Assert.Contains("inventory", exception.Message);
	}

	[Fact]
	public void Koalesce_WhenDuplicateVirtualPrefixWithDifferentCasing_ShouldThrowValidationException()
	{
		// Arrange - VirtualPrefix comparison should be case-insensitive
		var appSettingsStub = new
		{
			Koalesce = new CoreOptions
			{
				MergedEndpoint = "/v1/mergedapidefinition.json",
				Sources = new List<ApiSource>
				{
					new ApiSource
					{
						Url = "https://api1.com/v1/apidefinition.json",
						VirtualPrefix = "/Inventory"
					},
					new ApiSource
					{
						Url = "https://api2.com/v1/apidefinition.json",
						VirtualPrefix = "/inventory"  // Same prefix, different casing
					}
				}
			}
		};

		var configuration = ConfigurationHelper
			.BuildConfigurationFromObject(appSettingsStub);

		Services.AddKoalesce(configuration);

		var provider = Services.BuildServiceProvider();

		// Act & Assert
		var exception = Assert.Throws<OptionsValidationException>(() =>
		{
			var options = provider.GetRequiredService<IOptions<KoalesceOptions>>().Value;
		});

		Assert.Contains("Duplicate VirtualPrefix", exception.Message);
	}

	[Fact]
	public void Koalesce_WhenUniqueVirtualPrefixes_ShouldNotThrowException()
	{
		// Arrange
		var appSettingsStub = new
		{
			Koalesce = new CoreOptions
			{
				MergedEndpoint = "/v1/mergedapidefinition.json",
				Sources = new List<ApiSource>
				{
					new ApiSource
					{
						Url = "https://api1.com/v1/apidefinition.json",
						VirtualPrefix = "/inventory"
					},
					new ApiSource
					{
						Url = "https://api2.com/v1/apidefinition.json",
						VirtualPrefix = "/products"
					}
				}
			}
		};

		var configuration = ConfigurationHelper
			.BuildConfigurationFromObject(appSettingsStub);

		Services.AddKoalesce(configuration);

		var provider = Services.BuildServiceProvider();

		// Act & Assert - Should not throw
		var options = provider.GetRequiredService<IOptions<KoalesceOptions>>().Value;
		Assert.NotNull(options);
		Assert.Equal(2, options.Sources.Count);
	}

	[Fact]
	public void Koalesce_WhenMixedVirtualPrefixesWithEmptyOnes_ShouldNotThrowException()
	{
		// Arrange - Only non-empty VirtualPrefixes should be checked for duplicates
		var appSettingsStub = new
		{
			Koalesce = new CoreOptions
			{
				MergedEndpoint = "/v1/mergedapidefinition.json",
				Sources = new List<ApiSource>
				{
					new ApiSource
					{
						Url = "https://api1.com/v1/apidefinition.json",
						VirtualPrefix = null
					},
					new ApiSource
					{
						Url = "https://api2.com/v1/apidefinition.json",
						VirtualPrefix = ""  // Empty should be treated as "no prefix"
					},
					new ApiSource
					{
						Url = "https://api3.com/v1/apidefinition.json",
						VirtualPrefix = "/products"
					}
				}
			}
		};

		var configuration = ConfigurationHelper
			.BuildConfigurationFromObject(appSettingsStub);

		Services.AddKoalesce(configuration);

		var provider = Services.BuildServiceProvider();

		// Act & Assert - Should not throw (null and empty are not considered duplicates)
		var options = provider.GetRequiredService<IOptions<KoalesceOptions>>().Value;
		Assert.NotNull(options);
		Assert.Equal(3, options.Sources.Count);
	}

	#endregion

	#region HttpTimeoutSeconds Validation Tests

	[Fact]
	public void Koalesce_WithDefaultHttpTimeout_ShouldUseDefaultValue()
	{
		// Arrange
		var appSettingsStub = new
		{
			Koalesce = new
			{
				MergedEndpoint = "/v1/mergedapidefinition.json",
				Sources = new[]
				{
					new { Url = "https://api1.com/v1/apidefinition.json" }
				}
			}
		};

		var configuration = ConfigurationHelper
			.BuildConfigurationFromObject(appSettingsStub);

		Services.AddKoalesce(configuration);

		var provider = Services.BuildServiceProvider();

		// Act
		var options = provider.GetRequiredService<IOptions<KoalesceOptions>>().Value;

		// Assert - Default timeout
		Assert.Equal(CoreConstants.DefaultHttpTimeoutSeconds, options.HttpTimeoutSeconds);
	}

	[Fact]
	public void Koalesce_WithCustomHttpTimeout_ShouldBindCorrectly()
	{
		// Arrange
		var appSettingsStub = new
		{
			Koalesce = new
			{
				MergedEndpoint = "/v1/mergedapidefinition.json",
				Sources = new[]
				{
					new { Url = "https://api1.com/v1/apidefinition.json" }
				},
				HttpTimeoutSeconds = 30
			}
		};

		var configuration = ConfigurationHelper
			.BuildConfigurationFromObject(appSettingsStub);

		Services.AddKoalesce(configuration);

		var provider = Services.BuildServiceProvider();

		// Act
		var options = provider.GetRequiredService<IOptions<KoalesceOptions>>().Value;

		// Assert - Custom timeout
		Assert.Equal(30, options.HttpTimeoutSeconds);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	[InlineData(-100)]
	public void Koalesce_WithInvalidHttpTimeout_ShouldThrowValidationException(int invalidTimeout)
	{
		// Arrange
		var appSettingsStub = new
		{
			Koalesce = new
			{
				MergedEndpoint = "/v1/mergedapidefinition.json",
				Sources = new[]
				{
					new { Url = "https://api1.com/v1/apidefinition.json" }
				},
				HttpTimeoutSeconds = invalidTimeout
			}
		};

		var configuration = ConfigurationHelper
			.BuildConfigurationFromObject(appSettingsStub);

		Services.AddKoalesce(configuration);

		var provider = Services.BuildServiceProvider();

		// Act & Assert
		var exception = Assert.Throws<OptionsValidationException>(() =>
		{
			var options = provider.GetRequiredService<IOptions<KoalesceOptions>>().Value;
		});

		Assert.Contains(CoreConstants.HttpTimeoutMustBePositive, exception.Message);
	}

	#endregion

	#region Duplicate Source Validation Tests

	[Fact]
	public void Koalesce_WhenDuplicateSourceUrl_ShouldThrowValidationException()
	{
		// Arrange
		var appSettingsStub = new
		{
			Koalesce = new CoreOptions
			{
				MergedEndpoint = "/v1/mergedapidefinition.json",
				Sources = new List<ApiSource>
				{
					new ApiSource { Url = "https://api1.com/v1/apidefinition.json" },
					new ApiSource { Url = "https://api2.com/v1/apidefinition.json" },
					new ApiSource { Url = "https://api1.com/v1/apidefinition.json" } // Duplicate!
				}
			}
		};

		var configuration = ConfigurationHelper
			.BuildConfigurationFromObject(appSettingsStub);

		Services.AddKoalesce(configuration);

		var provider = Services.BuildServiceProvider();

		// Act & Assert
		var exception = Assert.Throws<OptionsValidationException>(() =>
		{
			var options = provider.GetRequiredService<IOptions<KoalesceOptions>>().Value;
		});

		Assert.Contains("Duplicate source", exception.Message);
		Assert.Contains("api1.com", exception.Message);
	}

	[Fact]
	public void Koalesce_WhenDuplicateSourceUrlWithDifferentCasing_ShouldThrowValidationException()
	{
		// Arrange - URL comparison should be case-insensitive
		var appSettingsStub = new
		{
			Koalesce = new CoreOptions
			{
				MergedEndpoint = "/v1/mergedapidefinition.json",
				Sources = new List<ApiSource>
				{
					new ApiSource { Url = "https://API1.COM/v1/apidefinition.json" },
					new ApiSource { Url = "https://api1.com/v1/apidefinition.json" } // Same URL, different casing
				}
			}
		};

		var configuration = ConfigurationHelper
			.BuildConfigurationFromObject(appSettingsStub);

		Services.AddKoalesce(configuration);

		var provider = Services.BuildServiceProvider();

		// Act & Assert
		var exception = Assert.Throws<OptionsValidationException>(() =>
		{
			var options = provider.GetRequiredService<IOptions<KoalesceOptions>>().Value;
		});

		Assert.Contains("Duplicate source", exception.Message);
	}

	[Fact]
	public void Koalesce_WhenDuplicateFilePath_ShouldThrowValidationException()
	{
		// Arrange - Create a temp file to pass file existence validation
		var tempFile = Path.GetTempFileName();
		try
		{
			var appSettingsStub = new
			{
				Koalesce = new CoreOptions
				{
					MergedEndpoint = "/v1/mergedapidefinition.json",
					Sources = new List<ApiSource>
					{
						new ApiSource { FilePath = tempFile },
						new ApiSource { FilePath = tempFile } // Duplicate!
					}
				}
			};

			var configuration = ConfigurationHelper
				.BuildConfigurationFromObject(appSettingsStub);

			Services.AddKoalesce(configuration);

			var provider = Services.BuildServiceProvider();

			// Act & Assert
			var exception = Assert.Throws<OptionsValidationException>(() =>
			{
				var options = provider.GetRequiredService<IOptions<KoalesceOptions>>().Value;
			});

			Assert.Contains("Duplicate source", exception.Message);
		}
		finally
		{
			File.Delete(tempFile);
		}
	}

	[Fact]
	public void Koalesce_WhenUniqueSources_ShouldNotThrowException()
	{
		// Arrange
		var appSettingsStub = new
		{
			Koalesce = new CoreOptions
			{
				MergedEndpoint = "/v1/mergedapidefinition.json",
				Sources = new List<ApiSource>
				{
					new ApiSource { Url = "https://api1.com/v1/apidefinition.json" },
					new ApiSource { Url = "https://api2.com/v1/apidefinition.json" },
					new ApiSource { Url = "https://api3.com/v1/apidefinition.json" }
				}
			}
		};

		var configuration = ConfigurationHelper
			.BuildConfigurationFromObject(appSettingsStub);

		Services.AddKoalesce(configuration);

		var provider = Services.BuildServiceProvider();

		// Act & Assert - Should not throw
		var options = provider.GetRequiredService<IOptions<KoalesceOptions>>().Value;
		Assert.NotNull(options);
		Assert.Equal(3, options.Sources.Count);
	}

	#endregion

	#region FilePath Source Validation Tests

	[Fact]
	public void Koalesce_WithBothUrlAndFilePath_ShouldThrowValidationException()
	{
		// Arrange
		var appSettingsStub = new
		{
			Koalesce = new
			{
				MergedEndpoint = "/v1/mergedapidefinition.json",
				Sources = new[]
				{
					new
					{
						Url = "https://api1.com/v1/apidefinition.json",
						FilePath = "C:/specs/api.json"
					}
				}
			}
		};

		var configuration = ConfigurationHelper
			.BuildConfigurationFromObject(appSettingsStub);

		Services.AddKoalesce(configuration);

		var provider = Services.BuildServiceProvider();

		// Act & Assert
		var exception = Assert.Throws<OptionsValidationException>(() =>
		{
			var options = provider.GetRequiredService<IOptions<KoalesceOptions>>().Value;
		});

		Assert.Contains("either Url or FilePath", exception.Message);
	}

	[Fact]
	public void Koalesce_WithNeitherUrlNorFilePath_ShouldThrowValidationException()
	{
		// Arrange
		var appSettingsStub = new
		{
			Koalesce = new
			{
				MergedEndpoint = "/v1/mergedapidefinition.json",
				Sources = new[]
				{
					new { VirtualPrefix = "/api" } // Neither Url nor FilePath
				}
			}
		};

		var configuration = ConfigurationHelper
			.BuildConfigurationFromObject(appSettingsStub);

		Services.AddKoalesce(configuration);

		var provider = Services.BuildServiceProvider();

		// Act & Assert
		var exception = Assert.Throws<OptionsValidationException>(() =>
		{
			var options = provider.GetRequiredService<IOptions<KoalesceOptions>>().Value;
		});

		Assert.Contains("either Url or FilePath", exception.Message);
	}

	#endregion
}