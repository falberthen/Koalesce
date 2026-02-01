namespace Koalesce.Tests.Base;

/// <summary>
/// Base class for Koalesce integration tests.
/// </summary>
[Collection("Koalesce Integration Tests")]
public abstract class KoalesceIntegrationTestBase : IAsyncLifetime
{
	protected Uri _gatewayUri = null!;
	protected WebApplication? _customersApi;
	protected WebApplication? _productsApi;
	protected WebApplication? _inventoryApi;
	protected HttpClient _httpClient = null!;

	public async Task InitializeAsync()
	{
		int port = GetAvailablePort();
		_gatewayUri = new Uri($"http://localhost:{port}");
		_httpClient = new HttpClient { BaseAddress = _gatewayUri };

		// Start the downstream APIs (Customers, Products & Inventory)
		_customersApi = RestAPIs.CustomersApi.Create();
		_productsApi = RestAPIs.ProductsApi.Create();
		_inventoryApi = RestAPIs.InventoryApi.Create();

		await _customersApi.StartAsync();
		await _productsApi.StartAsync();
		await _inventoryApi.StartAsync();
	}

	public async Task DisposeAsync()
	{
		await _customersApi!.StopAsync();
		await _productsApi!.StopAsync();
		await _inventoryApi!.StopAsync();
	}

	/// <summary>
	/// Starts a Koalesce-powered web application using the given configuration.
	/// </summary>
	protected async Task<WebApplication> StartWebApplicationAsync(string configFile, Action<WebApplicationBuilder> configureServices)
	{
		IConfiguration configuration = new ConfigurationBuilder()
			.SetBasePath(Directory.GetCurrentDirectory())
			.AddJsonFile(configFile, optional: false, reloadOnChange: false)
			.Build();

		WebApplicationBuilder builder = WebApplication.CreateBuilder();
		builder.WebHost.UseKestrel().UseUrls(_gatewayUri.AbsoluteUri);
		builder.Configuration.AddConfiguration(configuration);

		configureServices(builder); // Allow subclasses to register their own services

		var app = builder.Build();
		app.UseKoalesce();

		await app.StartAsync();
		return app;
	}

	/// <summary>
	/// Gets an available network port dynamically.
	/// </summary>
	protected static int GetAvailablePort()
	{
		using var tcpListener = new TcpListener(IPAddress.Loopback, 0);
		tcpListener.Start();
		int port = ((IPEndPoint)tcpListener.LocalEndpoint).Port;
		tcpListener.Stop();
		return port;
	}
}
