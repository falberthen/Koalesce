using Koalesce.Core.Options;

namespace Koalesce.Tests.Integration;

public partial class KoalesceForOpenApiTests : KoalesceIntegrationTestBase
{
	private const string _appSettings = "RestAPIs/appsettings.openapi.json";
	private const string _mergedOpenApiPath = "/swagger/v1/swagger.json";

	private const string _apiGatewaySettings = "RestAPIs/appsettings.apigateway.json";
	private const string _mergedApiGatewayPath = "/swagger/v1/apigateway.json";

	[Fact]
	public async Task KoalesceForOpenAPI_ShouldMergeOpenAPIRoutes()
	{
		// Arrange & Act
		var koalescingApi = await StartWebApplicationAsync(_appSettings,
			builder => builder.Services
				.AddKoalesce(builder.Configuration)
				.ForOpenAPI());

		var mergedResult = await _httpClient.GetStringAsync(_mergedOpenApiPath);

		Assert.False(string.IsNullOrWhiteSpace(mergedResult), "Merged API response is empty!");
		Assert.Contains("/api/customers", mergedResult);
		Assert.Contains("/api/products", mergedResult);

		await koalescingApi.StopAsync();
	}

	[Fact]
	public async Task KoalesceForOpenAPI_ShouldReturnValidOpenApiSchema()
	{
		// Arrange & Act
		var koalescingApi = await StartWebApplicationAsync(_appSettings,
			builder => builder.Services
				.AddKoalesce(builder.Configuration)
				.ForOpenAPI());

		var response = await _httpClient.GetAsync(_mergedOpenApiPath);

		response.EnsureSuccessStatusCode();
		var mergedResult = await response.Content.ReadAsStringAsync();

		Assert.False(string.IsNullOrWhiteSpace(mergedResult), "API response is empty!");
		Assert.Contains("\"openapi\": \"3.0.1\"", mergedResult);

		await koalescingApi.StopAsync();
	}

	[Fact]
	public async Task KoalesceForOpenAPI_ShouldIncludeCorrectServerUrls()
	{
		// Arrange & Act
		var koalescingApi = await StartWebApplicationAsync(_appSettings,
			builder => builder.Services
				.AddKoalesce(builder.Configuration)
				.ForOpenAPI());

		var mergedResult = await _httpClient.GetStringAsync(_mergedOpenApiPath);

		Assert.False(string.IsNullOrWhiteSpace(mergedResult), "Merged API response is empty!");
		Assert.Contains("http://localhost:8001", mergedResult);
		Assert.Contains("http://localhost:8002", mergedResult);

		await koalescingApi.StopAsync();
	}

	[Fact]
	public async Task KoalesceForOpenAPI_ShouldContainTopLevelServers()
	{
		// Arrange & Act
		var koalescingApi = await StartWebApplicationAsync(_appSettings,
			builder => builder.Services
				.AddKoalesce(builder.Configuration)
				.ForOpenAPI());

		var mergedResult = await _httpClient.GetStringAsync(_mergedOpenApiPath);

		// Assert top-level servers exist
		Assert.Contains("servers", mergedResult, StringComparison.OrdinalIgnoreCase);
		Assert.Contains("http://localhost:8001", mergedResult);
		Assert.Contains("http://localhost:8002", mergedResult);

		await koalescingApi.StopAsync();
	}

	[Fact]
	public async Task KoalesceForOpenAPI_ShouldEnsureEachPathHasServers()
	{
		// Arrange & Act
		var koalescingApi = await StartWebApplicationAsync(_appSettings,
			builder => builder.Services
				.AddKoalesce(builder.Configuration)
				.ForOpenAPI());

		var mergedResult = await _httpClient.GetStringAsync(_mergedOpenApiPath);

		// Validate paths exist
		Assert.Contains("/api/customers", mergedResult);
		Assert.Contains("/api/products", mergedResult);

		// Ensure each path has its own "servers" entry
		Assert.Contains("\"/api/customers\": {", mergedResult);
		Assert.Contains("\"servers\":", mergedResult
			.Substring(mergedResult.IndexOf("\"/api/customers\": {", StringComparison.Ordinal)));

		Assert.Contains("\"/api/products\": {", mergedResult);
		Assert.Contains("\"servers\":", mergedResult
			.Substring(mergedResult.IndexOf("\"/api/products\": {", StringComparison.Ordinal)));

		await koalescingApi.StopAsync();
	}

	[Fact]
	public async Task KoalesceForOpenAPI_ShouldContainTagsPerPathWhenTagProvided()
	{
		// Arrange & Act
		var koalescingApi = await StartWebApplicationAsync(_appSettings,
			builder => builder.Services
				.AddKoalesce(builder.Configuration)
				.ForOpenAPI());

		var mergedResult = await _httpClient.GetStringAsync(_mergedOpenApiPath);

		// Assert top-level servers exist
		Assert.Contains("tags", mergedResult, StringComparison.OrdinalIgnoreCase);

		await koalescingApi.StopAsync();
	}

	[Fact]
	public async Task KoalesceForOpenAPI_ShouldPreserveDownstreamSecuritySchemes()
	{
		// Arrange & Act
		var koalescingApi = await StartWebApplicationAsync(_appSettings,
			builder => builder.Services
				.AddKoalesce(builder.Configuration)
				.ForOpenAPI());

		var mergedResult = await _httpClient.GetStringAsync(_mergedOpenApiPath);

		// Assert that security schemes from downstream APIs are preserved
		Assert.Contains("\"securitySchemes\"", mergedResult, StringComparison.OrdinalIgnoreCase);
		Assert.Contains("\"api_key\"", mergedResult, StringComparison.OrdinalIgnoreCase);
		
		// Operations with security keep it, operations without security remain public
		// No automatic inheritance or transformation is performed
		await koalescingApi.StopAsync();
	}

	[Fact]
	public async Task KoalesceForOpenAPI_WhenApiGatewayBaseUrlIsSet_ShouldMergeAllServerUrlDefinitionsIntoSingle()
	{
		// Arrange & Act
		var koalescingApi = await StartWebApplicationAsync(_apiGatewaySettings,
			builder => builder.Services
				.AddKoalesce(builder.Configuration)
				.ForOpenAPI());

		var mergedResult = await _httpClient.GetStringAsync(_mergedApiGatewayPath);

		// Ensure API response is not empty
		Assert.False(string.IsNullOrWhiteSpace(mergedResult), "Merged API response is empty!");

		//  Validate that only the API Gateway server is present
		Assert.Contains("\"servers\"", mergedResult);
		Assert.Contains("\"url\": \"http://localhost:5000\"", mergedResult);
		Assert.DoesNotContain("\"url\": \"http://localhost:8001\"", mergedResult);
		Assert.DoesNotContain("\"url\": \"http://localhost:8002\"", mergedResult);
		Assert.DoesNotContain("\"url\": \"http://localhost:8003\"", mergedResult);

		// Ensure `/api/customers` and `/api/products` and `/api/inventory` exist in the merged document
		Assert.Contains("\"/api/customers\": {", mergedResult);
		Assert.Contains("\"/api/products\": {", mergedResult);
		Assert.Contains("\"/inventory/api/products\": {", mergedResult);

		// Ensure each path does not have individual `servers` (since it's using API Gateway)
		Assert.DoesNotContain("\"servers\":", mergedResult
			.Substring(mergedResult.IndexOf("\"/api/customers\": {", StringComparison.Ordinal)));
		Assert.DoesNotContain("\"servers\":", mergedResult
			.Substring(mergedResult.IndexOf("\"/api/products\": {", StringComparison.Ordinal)));
		Assert.DoesNotContain("\"servers\":", mergedResult
			.Substring(mergedResult.IndexOf("\"/inventory/api/products\": {", StringComparison.Ordinal)));
		await koalescingApi.StopAsync();
	}

	#region TESTS USING SchemaConflictPattern

	private const string _schemaConflictSettings = "RestAPIs/appsettings.schemaconflict.json";
	private const string _bothVirtualPrefixSettings = "RestAPIs/appsettings.bothvirtualprefix.json";
	private const string _firstVirtualPrefixSettings = "RestAPIs/appsettings.firstvirtualprefix.json";

	[Fact]
	public async Task KoalesceForOpenAPI_WhenSchemaConflictOccurs_ShouldRenameOnlySourceWithVirtualPrefix()
	{
		// Arrange & Act
		var koalescingApi = await StartWebApplicationAsync(_appSettings,
			builder => builder.Services
				.AddKoalesce(builder.Configuration)
				.ForOpenAPI());

		var mergedResult = await _httpClient.GetStringAsync(_mergedOpenApiPath);

		// Assert: Default pattern is {Prefix}_{SchemaName}
		// VirtualPrefix acts as an explicit "namespace my schemas" directive.
		// When only ONE source has VirtualPrefix, THAT source's schema is renamed.
		// The source WITHOUT VirtualPrefix keeps the original name.
		// This is deterministic and independent of processing order.
		Assert.Contains("\"Product\"", mergedResult);           // Products API (no VirtualPrefix) keeps original
		Assert.Contains("\"Inventory_Product\"", mergedResult); // Inventory API (with VirtualPrefix) is renamed

		// Verify the reference was updated
		Assert.Contains("#/components/schemas/Inventory_Product", mergedResult);

		await koalescingApi.StopAsync();
	}

	[Fact]
	public async Task KoalesceForOpenAPI_WhenBothSourcesHaveVirtualPrefix_ShouldRenameBothConflictingSchemas()
	{
		// Arrange & Act
		var koalescingApi = await StartWebApplicationAsync(_bothVirtualPrefixSettings,
			builder => builder.Services
				.AddKoalesce(builder.Configuration)
				.ForOpenAPI());

		var mergedResult = await _httpClient.GetStringAsync(_mergedOpenApiPath);

		// Assert: When BOTH sources have VirtualPrefix and define the same schema (Product),
		// BOTH should be renamed to avoid order-dependent behavior
		// Products API (first) defines Product -> Products_Product
		// Inventory API (second) defines Product -> Inventory_Product
		Assert.Contains("\"Products_Product\"", mergedResult);
		Assert.Contains("\"Inventory_Product\"", mergedResult);

		// The original "Product" name should NOT exist (both were renamed)
		Assert.DoesNotContain("\"Product\":", mergedResult);

		// Verify the references were updated
		Assert.Contains("#/components/schemas/Products_Product", mergedResult);
		Assert.Contains("#/components/schemas/Inventory_Product", mergedResult);

		await koalescingApi.StopAsync();
	}

	[Fact]
	public async Task KoalesceForOpenAPI_WhenOnlyFirstSourceHasVirtualPrefix_ShouldRenameFirstAndKeepSecondOriginal()
	{
		// Arrange & Act
		// Products API (first) has VirtualPrefix "/products"
		// Inventory API (second) has NO VirtualPrefix
		var koalescingApi = await StartWebApplicationAsync(_firstVirtualPrefixSettings,
			builder => builder.Services
				.AddKoalesce(builder.Configuration)
				.ForOpenAPI());

		var mergedResult = await _httpClient.GetStringAsync(_mergedOpenApiPath);

		// Assert: VirtualPrefix is an explicit "namespace my schemas" directive.
		// The source WITH VirtualPrefix (Products) should be renamed.
		// The source WITHOUT VirtualPrefix (Inventory) keeps the original name.
		// This proves the behavior is deterministic regardless of processing order.
		Assert.Contains("\"Product\"", mergedResult);           // Inventory API (no VirtualPrefix) keeps original
		Assert.Contains("\"Products_Product\"", mergedResult);  // Products API (with VirtualPrefix) is renamed

		// Verify the references were updated
		Assert.Contains("#/components/schemas/Products_Product", mergedResult);

		await koalescingApi.StopAsync();
	}

	[Fact]
	public async Task KoalesceForOpenAPI_WhenSchemaConflictPatternIsCustomized_ShouldRenameSchemaWithCustomPattern()
	{
		// Arrange & Act: Using appsettings with SchemaConflictPattern = "{SchemaName}_{Prefix}"
		// Products API (first) has NO VirtualPrefix
		// Inventory API (second) has VirtualPrefix "/inventory"
		var koalescingApi = await StartWebApplicationAsync(_schemaConflictSettings,
			builder => builder.Services
				.AddKoalesce(builder.Configuration)
				.ForOpenAPI());

		var mergedResult = await _httpClient.GetStringAsync(_mergedOpenApiPath);

		// Assert: Custom pattern is {SchemaName}_{Prefix}
		// Only the source WITH VirtualPrefix (Inventory) is renamed using the custom pattern
		// Products (no VirtualPrefix) keeps the original name
		Assert.Contains("\"Product\"", mergedResult);           // Products keeps original
		Assert.Contains("\"Product_Inventory\"", mergedResult); // Inventory renamed with custom pattern

		// Verify the reference was updated
		Assert.Contains("#/components/schemas/Product_Inventory", mergedResult);

		await koalescingApi.StopAsync();
	}
	#endregion

	#region TESTS USING ExcludePaths

	private const string _excludePathsSettings = "RestAPIs/appsettings.excludepaths.json";
	private const string _mergedExcludePathsPath = "/swagger/v1/excludepaths.json";

	[Fact]
	public async Task KoalesceForOpenAPI_WhenExcludePathsConfigured_ShouldNotIncludeExcludedPaths()
	{
		// Arrange & Act
		var koalescingApi = await StartWebApplicationAsync(_excludePathsSettings,
			builder => builder.Services
				.AddKoalesce(builder.Configuration)
				.ForOpenAPI());

		var mergedResult = await _httpClient.GetStringAsync(_mergedExcludePathsPath);

		Assert.False(string.IsNullOrWhiteSpace(mergedResult), "Merged API response is empty!");

		// The "/api/customers" (list all) should still be present
		Assert.Contains("/api/customers", mergedResult);

		// The "/api/customers/{id}" path should be excluded
		Assert.DoesNotContain("/api/customers/{id}", mergedResult);

		// Products path should be present (not excluded)
		Assert.Contains("/api/products", mergedResult);

		await koalescingApi.StopAsync();
	}

	[Fact]
	public async Task KoalesceForOpenAPI_WhenExcludePathsWithWildcard_ShouldExcludeMatchingPaths()
	{
		// Arrange & Act
		var koalescingApi = await StartWebApplicationAsync(_appSettings,
			builder => builder.Services
				.AddKoalesce(builder.Configuration)
				.ForOpenAPI(options =>
				{
					// Programmatically configure ExcludePaths with wildcard
					if (options.Sources != null)
					{
						foreach (var source in options.Sources.Where(s => s.Url.Contains("8001")))
						{
							source.ExcludePaths = new List<string> { "/api/customers/*" };
						}
					}
				}));

		var mergedResult = await _httpClient.GetStringAsync(_mergedOpenApiPath);

		Assert.False(string.IsNullOrWhiteSpace(mergedResult), "Merged API response is empty!");

		// The "/api/customers" (exact match) should be excluded by wildcard
		Assert.DoesNotContain("\"/api/customers\":", mergedResult);

		// The "/api/customers/{id}" should also be excluded by wildcard
		Assert.DoesNotContain("/api/customers/{id}", mergedResult);

		// Products path should be present (not excluded)
		Assert.Contains("/api/products", mergedResult);

		await koalescingApi.StopAsync();
	}

	#endregion

	#region TESTS USING FailOnServiceLoadError

	[Fact]
	public async Task KoalesceForOpenAPI_WhenFailOnLoadErrorIsFalse_AndSourceIsUnreachable_ShouldSkipSourceAndReturnPartialResult()
	{
		// Arrange & Act
		// Using standard settings (valid sources) BUT injecting a "Bad Source" programmatically
		// and ensure the flag is FALSE (Resilient Mode)
		var koalescingApi = await StartWebApplicationAsync(_appSettings,
			builder => builder.Services
				.AddKoalesce(builder.Configuration)
				.ForOpenAPI(options =>
				{
					options.FailOnServiceLoadError = false; // Default behavior

					// Inject a URL that definitely doesn't exist
					options.Sources ??= new List<ApiSource>();
					options.Sources.Add(new ApiSource
					{
						Url = "http://localhost:54321/non-existent/swagger.json",
						VirtualPrefix = "/ghost"
					});
				}));

		var response = await _httpClient.GetAsync(_mergedOpenApiPath);
		var mergedResult = await response.Content.ReadAsStringAsync();

		// Assert
		// Should still be 200 OK (Resilience)
		Assert.True(response.IsSuccessStatusCode, "API should return success even with one dead source.");

		// Should contain content from the VALID sources (from _appSettings)
		Assert.Contains("/api/customers", mergedResult);
		Assert.Contains("/api/products", mergedResult);

		// Should NOT contain content from the INVALID source
		Assert.DoesNotContain("/ghost", mergedResult);

		await koalescingApi.StopAsync();
	}

	[Fact]
	public async Task KoalesceForOpenAPI_WhenFailOnLoadErrorIsTrue_AndSourceIsUnreachable_ShouldReturnInternalServerError()
	{
		// Arrange & Act
		// Inject a "Bad Source" and ensure the flag is TRUE (Strict Mode)
		var koalescingApi = await StartWebApplicationAsync(_appSettings,
			builder => builder.Services
				.AddKoalesce(builder.Configuration)
				.ForOpenAPI(options =>
				{
					options.FailOnServiceLoadError = true; // STRICT MODE

					// Inject a URL that definitely doesn't exist
					options.Sources ??= new List<ApiSource>();
					options.Sources.Add(new ApiSource
					{
						Url = "http://localhost:54321/non-existent/swagger.json"
					});
				}));

		var response = await _httpClient.GetAsync(_mergedOpenApiPath);

		// Assert
		// Should fail with 500 Internal Server Error 
		// (Because KoalescePathCouldNotBeLoadedException is thrown and caught by ASP.NET middleware)
		Assert.Equal(System.Net.HttpStatusCode.InternalServerError, response.StatusCode);

		await koalescingApi.StopAsync();
	}

	#endregion
}