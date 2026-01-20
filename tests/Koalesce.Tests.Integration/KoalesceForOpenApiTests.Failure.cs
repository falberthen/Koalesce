namespace Koalesce.Tests.Integration;

public partial class KoalesceForOpenApiTests
{
	const string _identicalPathSettings = "RestAPIs/appsettings.identicalpaths.json";	

	[Fact]
	public async Task KoalesceForOpenAPI_WhenIdenticalPaths_AndSkipIdenticalPathsIsFalse_ShouldReturnHttp500()
	{
		// Arrange
		var koalescingApi = await StartWebApplicationAsync(_identicalPathSettings, builder =>
		{
			builder.Services
				.AddKoalesce(builder.Configuration)
				.ForOpenAPI();
		});

		// Act
		var response = await _httpClient.GetAsync(_mergedApiGatewayPath);
		var responseContent = await response.Content.ReadAsStringAsync();

		// Assert
		Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
		Assert.Contains("Identical path", responseContent);

		await koalescingApi.StopAsync();
	}

	#region TESTS USING OpenApiSecurityScheme
	[Fact]
	public async Task KoalesceForOpenAPI_WhenUsingOpenApiSecurityScheme_WithEmptyName_ShouldReturn500()
	{
		// Arrange
		var koalescingApi = await StartWebApplicationAsync(_apiGatewaySettings, builder =>
		{
			builder.Services
				.AddKoalesce(builder.Configuration)
				.ForOpenAPI(options =>
				{
					options.OpenApiSecurityScheme = new OpenApiSecurityScheme
					{
						Name = string.Empty, // Invalid - empty name
						Type = SecuritySchemeType.Http,
						Scheme = "bearer"
					};
				});
		});

		// Act & Assert - Should get HTTP 500 because validation fails during merge
		var exception = await Assert.ThrowsAsync<HttpRequestException>(async () =>
		{
			await _httpClient.GetStringAsync(_mergedApiGatewayPath);
		});

		// Verify it's a 500 error caused by InvalidOperationException during merge
		Assert.Contains("500", exception.Message);

		await koalescingApi.StopAsync();
	}
	#endregion
}