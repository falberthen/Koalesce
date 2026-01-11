namespace Koalesce.Tests.Integration;

public partial class KoalesceForOpenApiTests
{
	const string _identicalPathSettings = "RestAPIs/appsettings.identicalpaths.json";	

	[Fact]
	public async Task Koalesce_WhenForOpenAPI_WithIdenticalPaths_AndSkipIdenticalPathsIsFalse_ShouldReturnHttp500()
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
}