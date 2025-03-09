using Kiota.Builder.SearchProviders.GitHub.Authentication;
using Koalesce.Samples.Kiota.GeneratedClient;
using Microsoft.Kiota.Http.HttpClientLibrary;

namespace Koalesce.Samples.Kiota;

public class ApiWrapper
{
	private readonly ApiClient _apiClient;
	private readonly string _openApiSpecPath;

	public ApiWrapper(KoalesceOptions koalesceOptions)
	{
		if (string.IsNullOrEmpty(koalesceOptions.ApiGatewayBaseUrl))
			throw new ArgumentNullException(nameof(koalesceOptions), "GatewayBaseUrl cannot be null.");

		_openApiSpecPath = $"{koalesceOptions.ApiGatewayBaseUrl}{koalesceOptions.MergedOpenApiPath}";

		// Initialize Kiota request adapter with anonymous authentication
		var requestAdapter = new HttpClientRequestAdapter(new AnonymousAuthenticationProvider())
		{
			BaseUrl = koalesceOptions.ApiGatewayBaseUrl
		};

		// Initialize API client
		_apiClient = new ApiClient(requestAdapter);
	}

	/// <summary>
	/// Fetches and displays customers & products from the API.
	/// </summary>
	public async Task ShowKoalescedResultAsync(bool generateClient = false)
	{
		if (generateClient)
			await KiotaClientBuilder.BuildAsync(_openApiSpecPath);

		// Execute API requests in parallel
		await Task.WhenAll(
			FetchAndDisplayDataAsync("Customers", () => _apiClient.Api.Customers.GetAsync()),
			FetchAndDisplayDataAsync("Products", () => _apiClient.Api.Products.GetAsync())
		);
	}

	/// <summary>
	/// Fetches API data and displays it in the console.
	/// </summary>
	private async Task FetchAndDisplayDataAsync<T>(string entityName, Func<Task<List<T>?>> fetchFunc) 
		where T : class
	{
		try
		{
			var items = await fetchFunc();
			if (items != null && items.Any())
			{
				Console.WriteLine($"\n - {entityName}:");
				foreach (var item in items)
				{
					// Use reflection to get properties dynamically
					var properties = typeof(T).GetProperties();
					var values = properties
						.Select(p => $"{p.Name}: {p.GetValue(item)}")
						.ToArray();

					Console.WriteLine($"  - {string.Join(", ", values)}");
				}
			}
			else
			{
				Console.WriteLine($"No {entityName.ToLower()} found.");
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error fetching {entityName.ToLower()}: {ex.Message}");
		}
	}
}
