using Koalesce.Core.Exceptions;

namespace Koalesce.Tests.Integration;

[Collection("Koalesce Integration Tests")]
public class KoalesceForOpenApiTests : KoalesceIntegrationTestBase
{
	const string appSettings = "appsettings.openapi.json";
	const string mergedOpenApiPath = "/swagger/v1/swagger.json";

	const string apiGatewaySettings = "appsettings.apigateway.json";
	const string mergedApiGatewayPath = "/swagger/v1/apigateway.json";

	const string identicalPathSettings = "appsettings.identicalpaths.json";

	[Fact]
	public async Task Koalesce_WhenForOpenAPI_ShouldMergeOpenAPIRoutes()
	{
		// Arrange & Act
		var koalescingApi = await StartWebApplicationAsync(appSettings,
			builder => builder.Services
				.AddKoalesce(builder.Configuration)
				.ForOpenAPI());

		var mergedResult = await _httpClient.GetStringAsync(mergedOpenApiPath);

		Assert.False(string.IsNullOrWhiteSpace(mergedResult), "Merged API response is empty!");
		Assert.Contains("/api/customers", mergedResult);
		Assert.Contains("/api/products", mergedResult);

		await koalescingApi.StopAsync();
	}

	[Fact]
	public async Task Koalesce_WhenForOpenAPI_ShouldReturnValidOpenApiSchema()
	{
		// Arrange & Act
		var koalescingApi = await StartWebApplicationAsync(appSettings,
			builder => builder.Services
				.AddKoalesce(builder.Configuration)
				.ForOpenAPI());

		var response = await _httpClient.GetAsync(mergedOpenApiPath);

		response.EnsureSuccessStatusCode();
		var mergedResult = await response.Content.ReadAsStringAsync();

		Assert.False(string.IsNullOrWhiteSpace(mergedResult), "API response is empty!");
		Assert.Contains("\"openapi\": \"3.0.1\"", mergedResult);

		await koalescingApi.StopAsync();
	}

	[Fact]
	public async Task Koalesce_WhenForOpenAPI_ShouldIncludeCorrectServerUrls()
	{
		// Arrange & Act
		var koalescingApi = await StartWebApplicationAsync(appSettings,
			builder => builder.Services
				.AddKoalesce(builder.Configuration)
				.ForOpenAPI());

		var mergedResult = await _httpClient.GetStringAsync(mergedOpenApiPath);

		Assert.False(string.IsNullOrWhiteSpace(mergedResult), "Merged API response is empty!");
		Assert.Contains("http://localhost:8001", mergedResult);
		Assert.Contains("http://localhost:8002", mergedResult);

		await koalescingApi.StopAsync();
	}

	[Fact]
	public async Task Koalesce_WhenForOpenAPI_ShouldContainTopLevelServers()
	{
		// Arrange & Act
		var koalescingApi = await StartWebApplicationAsync(appSettings,
			builder => builder.Services
				.AddKoalesce(builder.Configuration)
				.ForOpenAPI());

		var mergedResult = await _httpClient.GetStringAsync(mergedOpenApiPath);

		// Assert top-level servers exist
		Assert.Contains("servers", mergedResult, StringComparison.OrdinalIgnoreCase);
		Assert.Contains("http://localhost:8001", mergedResult);
		Assert.Contains("http://localhost:8002", mergedResult);

		await koalescingApi.StopAsync();
	}

	[Fact]
	public async Task Koalesce_WhenForOpenAPI_ShouldEnsureEachPathHasServers()
	{
		// Arrange & Act
		var koalescingApi = await StartWebApplicationAsync(appSettings,
			builder => builder.Services
				.AddKoalesce(builder.Configuration)
				.ForOpenAPI());

		var mergedResult = await _httpClient.GetStringAsync(mergedOpenApiPath);

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
	public async Task Koalesce_WhenForOpenAPI_ShouldContainTagsPerPathWhenTagProvided()
	{
		// Arrange & Act
		var koalescingApi = await StartWebApplicationAsync(appSettings,
			builder => builder.Services
				.AddKoalesce(builder.Configuration)
				.ForOpenAPI());

		var mergedResult = await _httpClient.GetStringAsync(mergedOpenApiPath);

		// Assert top-level servers exist
		Assert.Contains("tags", mergedResult, StringComparison.OrdinalIgnoreCase);		

		await koalescingApi.StopAsync();
	}

	[Fact]
	public async Task Koalesce_WhenForOpenAPI_ShouldMergeSecuritySchemes()
	{
		// Arrange & Act
		var koalescingApi = await StartWebApplicationAsync(appSettings,
			builder => builder.Services
				.AddKoalesce(builder.Configuration)
				.ForOpenAPI());

		var mergedResult = await _httpClient.GetStringAsync(mergedOpenApiPath);

		// Assert that security schemes exist
		Assert.Contains("\"securitySchemes\"", mergedResult, StringComparison.OrdinalIgnoreCase);
		Assert.Contains("\"api_key\"", mergedResult, StringComparison.OrdinalIgnoreCase);

		// Assert that security requirements exist in at least one path
		Assert.Contains("\"security\":", mergedResult, StringComparison.OrdinalIgnoreCase);

		await koalescingApi.StopAsync();
	}

	[Fact]
	public async Task Koalesce_WhenUsingApiGateway_ShouldMergeProductsAndCustomersWithSingleServer()
	{
		// Arrange & Act
		var koalescingApi = await StartWebApplicationAsync(apiGatewaySettings,
			builder => builder.Services
				.AddKoalesce(builder.Configuration)
				.ForOpenAPI());

		var mergedResult = await _httpClient.GetStringAsync(mergedApiGatewayPath);

		// Ensure API response is not empty
		Assert.False(string.IsNullOrWhiteSpace(mergedResult), "Merged API response is empty!");

		//  Validate that only the API Gateway server is present
		Assert.Contains("\"servers\"", mergedResult);
		Assert.Contains("\"url\": \"http://localhost:5000\"", mergedResult);
		Assert.DoesNotContain("\"url\": \"http://localhost:8001\"", mergedResult);
		Assert.DoesNotContain("\"url\": \"http://localhost:8002\"", mergedResult);

		// Ensure `/api/customers` and `/api/products` exist in the merged document
		Assert.Contains("\"/api/customers\": {", mergedResult);
		Assert.Contains("\"/api/products\": {", mergedResult);

		// Ensure each path does not have individual `servers` (since it's using API Gateway)
		Assert.DoesNotContain("\"servers\":", mergedResult
			.Substring(mergedResult.IndexOf("\"/api/customers\": {", StringComparison.Ordinal)));
		Assert.DoesNotContain("\"servers\":", mergedResult
			.Substring(mergedResult.IndexOf("\"/api/products\": {", StringComparison.Ordinal)));

		await koalescingApi.StopAsync();
	}

	[Fact]
	public async Task Koalesce_WhenIdenticalPathsExist_AndSkipIdenticalPathsIsFalse_ShouldReturnHttp500()
	{
		// Arrange
		var koalescingApi = await StartWebApplicationAsync(identicalPathSettings, builder =>
		{
			builder.Services
				.AddKoalesce(builder.Configuration)
				.ForOpenAPI();
		});

		// Act
		var response = await _httpClient.GetAsync(mergedApiGatewayPath);
		var responseContent = await response.Content.ReadAsStringAsync();

		// Assert
		Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
		Assert.Contains("Identical paths detected:", responseContent);

		await koalescingApi.StopAsync();
	}
}