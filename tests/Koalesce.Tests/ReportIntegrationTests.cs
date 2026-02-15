namespace Koalesce.Tests;

public class ReportIntegrationTests : KoalesceIntegrationTestBase
{
	private const string _mergedEndpoint = "/swagger/v1/swagger.json";
	private const string _reportEndpoint = "/koalesce/report.json";
	private const string _reportHtmlEndpoint = "/koalesce/report.html";

	private static readonly JsonSerializerOptions _jsonOptions = new()
	{
		PropertyNameCaseInsensitive = true
	};

	#region Report is a byproduct of merge (read-only)

	[Fact]
	public async Task Report_BeforeMerge_ShouldReturnEmptyJson()
	{
		// Arrange: start app but do NOT hit the merged endpoint
		var koalescingApi = await StartWebApplicationAsync("RestAPIs/appsettings.openapi.json",
			builder => builder.Services
				.AddKoalesce(builder.Configuration, options =>
				{
					options.MergeReportEndpoint = _reportEndpoint;
				}));

		// Act: hit the report endpoint directly (no merge has occurred)
		var reportJson = await _httpClient.GetStringAsync(_reportEndpoint);

		// Assert: empty JSON since no merge happened yet
		Assert.Equal("{}", reportJson);

		await koalescingApi.StopAsync();
	}

	[Fact]
	public async Task Report_AfterMerge_ShouldContainData()
	{
		var koalescingApi = await StartWebApplicationAsync("RestAPIs/appsettings.openapi.json",
			builder => builder.Services
				.AddKoalesce(builder.Configuration, options =>
				{
					options.MergeReportEndpoint = _reportEndpoint;
				}));

		var report = await GetReportAfterMergeAsync();

		Assert.True(report.Summary.TotalSources > 0);
		Assert.NotEmpty(report.Sources);

		await koalescingApi.StopAsync();
	}

	#endregion

	#region Standard Merge Report

	[Fact]
	public async Task Report_StandardMerge_ShouldContainCorrectSourceCounts()
	{
		var koalescingApi = await StartWebApplicationAsync("RestAPIs/appsettings.openapi.json",
			builder => builder.Services
				.AddKoalesce(builder.Configuration, options =>
				{
					options.MergeReportEndpoint = _reportEndpoint;
				}));

		var report = await GetReportAfterMergeAsync();

		// 3 sources, all loaded
		Assert.Equal(3, report.Summary.TotalSources);
		Assert.Equal(3, report.Summary.LoadedSources);
		Assert.Null(report.Summary.FailedSources); // 0 → omitted
		Assert.Equal(3, report.Sources.Count);
		Assert.All(report.Sources, s => Assert.True(s.IsLoaded));

		await koalescingApi.StopAsync();
	}

	[Fact]
	public async Task Report_StandardMerge_ShouldTrackPathsMerged()
	{
		var koalescingApi = await StartWebApplicationAsync("RestAPIs/appsettings.openapi.json",
			builder => builder.Services
				.AddKoalesce(builder.Configuration, options =>
				{
					options.MergeReportEndpoint = _reportEndpoint;
				}));

		var report = await GetReportAfterMergeAsync();

		// /api/customers, /api/customers/{id}, /api/products, /inventory/api/products = 4 paths
		Assert.Equal(4, report.Summary.TotalPathsMerged);

		await koalescingApi.StopAsync();
	}

	[Fact]
	public async Task Report_StandardMerge_ShouldTrackSchemaConflicts()
	{
		// Products and Inventory both define "Product" schema — conflict resolved by renaming
		var koalescingApi = await StartWebApplicationAsync("RestAPIs/appsettings.openapi.json",
			builder => builder.Services
				.AddKoalesce(builder.Configuration, options =>
				{
					options.MergeReportEndpoint = _reportEndpoint;
				}));

		var report = await GetReportAfterMergeAsync();

		Assert.NotNull(report.Summary.SchemaConflictsResolved);
		Assert.True(report.Summary.SchemaConflictsResolved >= 1);
		Assert.NotNull(report.Conflicts);
		Assert.NotNull(report.Conflicts.Schemas);
		Assert.Contains(report.Conflicts.Schemas, c => c.OriginalKey == "Product");

		await koalescingApi.StopAsync();
	}

	[Fact]
	public async Task Report_StandardMerge_ShouldHaveTimestamp()
	{
		var koalescingApi = await StartWebApplicationAsync("RestAPIs/appsettings.openapi.json",
			builder => builder.Services
				.AddKoalesce(builder.Configuration, options =>
				{
					options.MergeReportEndpoint = _reportEndpoint;
				}));

		var report = await GetReportAfterMergeAsync();

		Assert.True(report.Timestamp > DateTimeOffset.UtcNow.AddMinutes(-1));
		Assert.True(report.Timestamp <= DateTimeOffset.UtcNow.AddSeconds(5));

		await koalescingApi.StopAsync();
	}

	[Fact]
	public async Task Report_StandardMerge_ShouldOmitEmptySections()
	{
		var koalescingApi = await StartWebApplicationAsync("RestAPIs/appsettings.openapi.json",
			builder => builder.Services
				.AddKoalesce(builder.Configuration, options =>
				{
					options.MergeReportEndpoint = _reportEndpoint;
				}));

		var report = await GetReportAfterMergeAsync();

		// No dedup in standard merge (different security scheme types)
		Assert.Null(report.Deduplication);
		// No excluded paths in standard merge
		Assert.Null(report.Summary.PathsExcluded);

		await koalescingApi.StopAsync();
	}

	#endregion

	#region Exclusions Report

	[Fact]
	public async Task Report_WithExcludePaths_ShouldTrackExcludedPaths()
	{
		var koalescingApi = await StartWebApplicationAsync("RestAPIs/appsettings.excludepaths.json",
			builder => builder.Services
				.AddKoalesce(builder.Configuration, options =>
				{
					options.MergeReportEndpoint = _reportEndpoint;
				}));

		// Trigger merge via the correct merged endpoint
		await _httpClient.GetStringAsync("/swagger/v1/excludepaths.json");
		var reportJson = await _httpClient.GetStringAsync(_reportEndpoint);
		var report = JsonSerializer.Deserialize<MergeReport>(reportJson, _jsonOptions)!;

		Assert.Equal(1, report.Summary.PathsExcluded);
		Assert.NotNull(report.Removals);
		Assert.NotNull(report.Removals.ExcludedPaths);
		Assert.Single(report.Removals.ExcludedPaths);
		Assert.Equal("/api/customers/{id}", report.Removals.ExcludedPaths[0].Path);

		await koalescingApi.StopAsync();
	}

	#endregion

	#region Security Scheme Conflict Report

	[Fact]
	public async Task Report_SecuritySchemeConflict_ShouldTrackConflictResolution()
	{
		var koalescingApi = await StartWebApplicationAsync("RestAPIs/appsettings.securityconflict-vp.json",
			builder => builder.Services
				.AddKoalesce(builder.Configuration, options =>
				{
					options.MergeReportEndpoint = _reportEndpoint;
				}));

		var report = await GetReportAfterMergeAsync();

		Assert.Equal(1, report.Summary.SecuritySchemeConflictsResolved);
		Assert.NotNull(report.Conflicts);
		Assert.NotNull(report.Conflicts.SecuritySchemes);
		Assert.Single(report.Conflicts.SecuritySchemes);
		Assert.Equal("bearerAuth", report.Conflicts.SecuritySchemes[0].OriginalKey);

		await koalescingApi.StopAsync();
	}

	#endregion

	#region Security Scheme Deduplication Report

	[Fact]
	public async Task Report_SecuritySchemeIdentical_ShouldTrackDeduplication()
	{
		var koalescingApi = await StartWebApplicationAsync("RestAPIs/appsettings.securityidentical.json",
			builder => builder.Services
				.AddKoalesce(builder.Configuration, options =>
				{
					options.MergeReportEndpoint = _reportEndpoint;
				}));

		var report = await GetReportAfterMergeAsync();

		Assert.Equal(1, report.Summary.SecuritySchemesDeduplicated);
		Assert.Null(report.Summary.SecuritySchemeConflictsResolved);
		Assert.NotNull(report.Deduplication);
		Assert.Single(report.Deduplication.SecuritySchemes);
		Assert.Equal("bearerAuth", report.Deduplication.SecuritySchemes[0].Key);

		await koalescingApi.StopAsync();
	}

	#endregion

	#region Report Endpoint Behavior

	[Fact]
	public async Task Report_Endpoint_ShouldReturnApplicationJson()
	{
		var koalescingApi = await StartWebApplicationAsync("RestAPIs/appsettings.openapi.json",
			builder => builder.Services
				.AddKoalesce(builder.Configuration, options =>
				{
					options.MergeReportEndpoint = _reportEndpoint;
				}));

		await _httpClient.GetStringAsync(_mergedEndpoint);
		var response = await _httpClient.GetAsync(_reportEndpoint);

		response.EnsureSuccessStatusCode();
		Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

		await koalescingApi.StopAsync();
	}

	[Fact]
	public async Task Report_HtmlEndpoint_ShouldReturnHtml()
	{
		var koalescingApi = await StartWebApplicationAsync("RestAPIs/appsettings.openapi.json",
			builder => builder.Services
				.AddKoalesce(builder.Configuration, options =>
				{
					options.MergeReportEndpoint = _reportHtmlEndpoint;
				}));

		await _httpClient.GetStringAsync(_mergedEndpoint);
		var response = await _httpClient.GetAsync(_reportHtmlEndpoint);

		response.EnsureSuccessStatusCode();
		Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);

		var html = await response.Content.ReadAsStringAsync();
		Assert.Contains("Koalesce Merge Report", html);
		Assert.Contains("Sources", html);

		await koalescingApi.StopAsync();
	}

	[Fact]
	public async Task Report_WhenEndpointNotConfigured_ShouldReturn404()
	{
		var koalescingApi = await StartWebApplicationAsync("RestAPIs/appsettings.openapi.json",
			builder => builder.Services
				.AddKoalesce(builder.Configuration));

		var response = await _httpClient.GetAsync(_reportEndpoint);

		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

		await koalescingApi.StopAsync();
	}

	[Fact]
	public async Task Report_WithFailedSource_ShouldTrackFailedSources()
	{
		var koalescingApi = await StartWebApplicationAsync("RestAPIs/appsettings.openapi.json",
			builder => builder.Services
				.AddKoalesce(builder.Configuration, options =>
				{
					options.MergeReportEndpoint = _reportEndpoint;
					options.Sources.Add(new ApiSource
					{
						Url = "http://localhost:54321/non-existent/swagger.json",
						VirtualPrefix = "/ghost"
					});
				}));

		var report = await GetReportAfterMergeAsync();

		Assert.Equal(4, report.Summary.TotalSources);
		Assert.Equal(3, report.Summary.LoadedSources);
		Assert.Equal(1, report.Summary.FailedSources);

		var failedSource = report.Sources.First(s => !s.IsLoaded);
		Assert.NotNull(failedSource.ErrorMessage);

		await koalescingApi.StopAsync();
	}

	#endregion

	/// <summary>
	/// Helper: triggers a merge by hitting the merged endpoint, then fetches the report.
	/// </summary>
	private async Task<MergeReport> GetReportAfterMergeAsync(string reportPath = _reportEndpoint)
	{
		// Trigger the merge first
		await _httpClient.GetStringAsync(_mergedEndpoint);

		// Then read the report (read-only from cache)
		var reportJson = await _httpClient.GetStringAsync(reportPath);
		return JsonSerializer.Deserialize<MergeReport>(reportJson, _jsonOptions)!;
	}
}
