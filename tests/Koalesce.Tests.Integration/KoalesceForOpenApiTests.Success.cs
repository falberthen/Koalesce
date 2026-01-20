namespace Koalesce.Tests.Integration;

public partial class KoalesceForOpenApiTests : KoalesceIntegrationTestBase
{
	private const string _appSettings = "RestAPIs/appsettings.openapi.json";
	private const string _mergedOpenApiPath = "/swagger/v1/swagger.json";

	private const string _apiGatewaySettings = "RestAPIs/appsettings.apigateway.json";
	private const string _mergedApiGatewayPath = "/swagger/v1/apigateway.json";

	private const string _schemaConflictSettings = "RestAPIs/appsettings.schemaconflict.json";

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

		// In the simplified model, security requirements are preserved as-is from downstream APIs
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

	#region TESTS USING OpenApiSecurityScheme
	[Fact]
	public async Task KoalesceForOpenAPI_WhenUsingOpenApiSecurityScheme_WithJwtBearerGatewaySecurity_ShouldAddGlobalBearerGatewaySecurity()
	{
		// Arrange & Act
		var koalescingApi = await StartWebApplicationAsync(_apiGatewaySettings, builder =>
		{
			builder.Services
				.AddKoalesce(builder.Configuration)
				.ForOpenAPI(options =>
				{
					options.ApplyGlobalJwtBearerSecurityScheme();
				});
		});

		var mergedResult = await _httpClient.GetStringAsync(_mergedApiGatewayPath);

		// Checks schema definitions
		Assert.Contains("\"type\": \"http\"", mergedResult);
		Assert.Contains("\"scheme\": \"bearer\"", mergedResult);

		// Confirms default description was applied
		Assert.Contains(OpenAPIConstants.JwtBearerSchemeDefaultDescription, mergedResult);

		// Confirms no scopes defined (Robust check using Regex to ignore whitespace)
		Assert.Contains(OpenAPIConstants.JwtBearerSchemeDefaultName, mergedResult);

		await koalescingApi.StopAsync();
	}

	[Fact]
	public async Task KoalesceForOpenAPI_WhenUsingOpenApiSecurityScheme_WithApiKeyGatewaySecurity_ShouldAddGlobalApiKeyGatewaySecurity()
	{
		// Arrange & Act
		var koalescingApi = await StartWebApplicationAsync(_apiGatewaySettings, builder =>
		{
			builder.Services
				.AddKoalesce(builder.Configuration)
				.ForOpenAPI(options =>
				{
					options.ApplyGlobalApiKeySecurityScheme("X-API-KEY");
				});
		});

		var mergedResult = await _httpClient.GetStringAsync(_mergedApiGatewayPath);

		// Confirms schema definitions for API Key
		Assert.Contains("\"type\": \"apiKey\"", mergedResult);
		Assert.Contains("\"in\": \"header\"", mergedResult);
		Assert.Contains("\"name\": \"X-API-KEY\"", mergedResult);

		// Confirms default description was applied
		Assert.Contains(OpenAPIConstants.ApiKeySchemeDefaultDescription, mergedResult);

		// Confirms global security application
		Assert.Matches("\"X-API-KEY\"\\s*:\\s*\\[\\s*\\]", mergedResult);

		await koalescingApi.StopAsync();
	}

	[Fact]
	public async Task KoalesceForOpenAPI_WhenUsingOpenApiSecurityScheme_WithBasicGatewaySecurity_ShouldAddGlobalBasicGatewaySecurity()
	{
		// Arrange & Act
		var koalescingApi = await StartWebApplicationAsync(_apiGatewaySettings, builder =>
		{
			builder.Services
				.AddKoalesce(builder.Configuration)
				.ForOpenAPI(options =>
				{
					// Configura para usar Basic Authentication
					options.ApplyGlobalBasicAuthSecurityScheme("Basic Auth");
				});
		});

		var mergedResult = await _httpClient.GetStringAsync(_mergedApiGatewayPath);

		// Confirms schema definitions for Basic Auth
		Assert.Contains("\"Basic Auth\"", mergedResult);
		Assert.Contains("\"type\": \"http\"", mergedResult);
		Assert.Contains("\"scheme\": \"basic\"", mergedResult); // Diferenciador chave do Bearer

		// Confirms default description was applied		
		Assert.Contains(OpenAPIConstants.BasicAuthSchemeDefaultDescription, mergedResult);

		// Confirms global security application
		Assert.Contains(OpenAPIConstants.BasicAuthSchemeDefaultName, mergedResult);

		await koalescingApi.StopAsync();
	}

	[Fact]
	public async Task KoalesceForOpenAPI_WhenUsingOpenApiSecurityScheme_WithOAuth2ClientCredentials_ShouldAddGlobalOAuth2Security()
	{
		// Arrange & Act
		var koalescingApi = await StartWebApplicationAsync(_apiGatewaySettings, builder =>
		{
			builder.Services
				.AddKoalesce(builder.Configuration)
				.ForOpenAPI(options =>
				{
					options.ApplyGlobalOAuth2ClientCredentialsSecurityScheme(
						tokenUrl: new Uri("https://localhost:5001/connect/token"),
						scopes: new Dictionary<string, string>
						{
						{ "my_api.read", "Read Access" }
						}
					);
				});
		});

		var mergedResult = await _httpClient.GetStringAsync(_mergedApiGatewayPath);

		// Confirms schema definitions for OAuth2 Client Credentials
		Assert.Contains("\"type\": \"oauth2\"", mergedResult);
		Assert.Contains("\"clientCredentials\":", mergedResult);

		// Confirms default description was applied		
		Assert.Contains(OpenAPIConstants.OAuth2ClientCredentialsSchemeDefaultDescription, mergedResult);

		// Confirms correct tokenUrl
		Assert.Contains("\"tokenUrl\": \"https://localhost:5001/connect/token\"", mergedResult);

		// Confirms correct scopes
		Assert.Contains("\"my_api.read\": \"Read Access\"", mergedResult);

		// Confirms global security application
		Assert.Contains(OpenAPIConstants.OAuth2ClientCredentialsSchemeDefaultName, mergedResult);

		await koalescingApi.StopAsync();
	}

	[Fact]
	public async Task KoalesceForOpenAPI_WhenUsingOpenApiSecurityScheme_ApplyOAuth2AuthCodeSecurityScheme_ShouldAddGlobalOAuth2Security()
	{
		// Arrange & Act
		var authUrl = "https://localhost:5001/connect/authorize";
		var tokenUrl = "https://localhost:5001/connect/token";

		var koalescingApi = await StartWebApplicationAsync(_apiGatewaySettings, builder =>
		{
			builder.Services
				.AddKoalesce(builder.Configuration)
				.ForOpenAPI(options =>
				{
					options.ApplyGlobalOAuth2AuthCodeSecurityScheme(
						authorizationUrl: new Uri(authUrl),
						tokenUrl: new Uri(tokenUrl),
						scopes: new Dictionary<string, string>
						{
							{ "openid", "OpenID Connect" },
							{ "profile", "User Profile" },
							{ "my_api.full_access", "Full API Access" }
						}
					);
				});
		});

		var mergedResult = await _httpClient.GetStringAsync(_mergedApiGatewayPath);

		// Confirms schema definitions for OAuth2 Client Credentials
		Assert.Contains("\"type\": \"oauth2\"", mergedResult);

		// Confirms default description was applied		
		Assert.Contains(OpenAPIConstants.OAuth2AuthCodeSchemeDefaultDescription, mergedResult);

		// Confirms authorization code flow
		Assert.Contains("\"authorizationCode\":", mergedResult);

		// Confirms correct URLs
		Assert.Contains($"\"authorizationUrl\": \"{authUrl}\"", mergedResult);
		Assert.Contains($"\"tokenUrl\": \"{tokenUrl}\"", mergedResult);

		// Confirms correct scopes
		Assert.Contains("\"openid\": \"OpenID Connect\"", mergedResult);
		Assert.Contains("\"profile\": \"User Profile\"", mergedResult);
		Assert.Contains("\"my_api.full_access\": \"Full API Access\"", mergedResult);

		// Confirms global security application
		Assert.Contains(OpenAPIConstants.OAuth2AuthCodeSchemeDefaultName, mergedResult);

		await koalescingApi.StopAsync();
	}

	[Fact]
	public async Task KoalesceForOpenAPI_WhenUsingOpenApiSecurityScheme_WithOpenIdConnectGatewaySecurity_ShouldAddGlobalOIDCSecurity()
	{
		// Arrange & Act
		var discoveryUrl = "https://localhost:5001/.well-known/openid-configuration";

		var koalescingApi = await StartWebApplicationAsync(_apiGatewaySettings, builder =>
		{
			builder.Services
				.AddKoalesce(builder.Configuration)
				.ForOpenAPI(options =>
				{
					options.ApplyGlobalOpenIdConnectSecurityScheme(
						openIdConnectUrl: new Uri(discoveryUrl)
					);
				});
		});

		var mergedResult = await _httpClient.GetStringAsync(_mergedApiGatewayPath);

		// Confirms schema definitions for OAuth2 Client Credentials
		Assert.Contains("\"type\": \"openIdConnect\"", mergedResult);
		Assert.Contains($"\"openIdConnectUrl\": \"{discoveryUrl}\"", mergedResult);

		// Confirms default description was applied		
		Assert.Contains(OpenAPIConstants.OpenIdConnectSchemeDefaultDescription, mergedResult);

		// Confirms global security application
		Assert.Contains(OpenAPIConstants.OpenIdConnectSchemeDefaultName, mergedResult);

		await koalescingApi.StopAsync();
	}
	#endregion

	#region TESTS USING SchemaConflictPattern
	[Fact]
	public async Task KoalesceForOpenAPI_WhenSchemaConflictOccurs_ShouldRenameSchemaWithDefaultPattern()
	{
		// Arrange & Act
		var koalescingApi = await StartWebApplicationAsync(_appSettings,
			builder => builder.Services
				.AddKoalesce(builder.Configuration)
				.ForOpenAPI());

		var mergedResult = await _httpClient.GetStringAsync(_mergedOpenApiPath);

		// Assert: Default pattern is {Prefix}_{SchemaName}, so Inventory's Product becomes Inventory_Product
		Assert.Contains("\"Product\"", mergedResult);
		Assert.Contains("\"Inventory_Product\"", mergedResult);

		// Verify the reference was updated
		Assert.Contains("#/components/schemas/Inventory_Product", mergedResult);

		await koalescingApi.StopAsync();
	}

	[Fact]
	public async Task KoalesceForOpenAPI_WhenSchemaConflictPatternIsCustomized_ShouldRenameSchemaWithCustomPattern()
	{
		// Arrange & Act: Using appsettings with SchemaConflictPattern = "{SchemaName}_{Prefix}"
		var koalescingApi = await StartWebApplicationAsync(_schemaConflictSettings,
			builder => builder.Services
				.AddKoalesce(builder.Configuration)
				.ForOpenAPI());

		var mergedResult = await _httpClient.GetStringAsync(_mergedOpenApiPath);

		// Assert: Custom pattern is {SchemaName}_{Prefix}, so Inventory's Product becomes Product_Inventory
		Assert.Contains("\"Product\"", mergedResult);
		Assert.Contains("\"Product_Inventory\"", mergedResult);

		// Verify the reference was updated
		Assert.Contains("#/components/schemas/Product_Inventory", mergedResult);

		await koalescingApi.StopAsync();
	}
	#endregion
}