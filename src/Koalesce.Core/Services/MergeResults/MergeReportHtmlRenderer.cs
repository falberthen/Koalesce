namespace Koalesce.Services.MergeResults;

/// <summary>
/// Renders a <see cref="MergeReportTemplate"/> as a user-friendly HTML page
/// using an embedded HTML template with placeholder replacement.
/// </summary>
public static class MergeReportHtmlRenderer
{
	private static readonly Lazy<string> _template = new(LoadTemplate);

	public static string Render(MergeReport report)
	{
		return _template.Value
			.Replace("{{Timestamp}}", Escape(report.Timestamp.ToString("yyyy-MM-dd HH:mm:ss") + " UTC"))
			.Replace("{{SummaryStats}}", BuildSummaryStats(report.Summary))
			.Replace("{{SourceRows}}", BuildSourceRows(report.Sources))
			.Replace("{{ConflictsSection}}", BuildConflictsSection(report.Conflicts))
			.Replace("{{DeduplicationSection}}", BuildDeduplicationSection(report.Deduplication))
			.Replace("{{RemovalsSection}}", BuildRemovalsSection(report.Removals));
	}

	private static string LoadTemplate()
	{
		var assembly = typeof(MergeReportHtmlRenderer).Assembly;
		var resourceName = assembly.GetManifestResourceNames()
			.First(n => n.EndsWith("MergeReportTemplate.html", StringComparison.OrdinalIgnoreCase));

		using var stream = assembly.GetManifestResourceStream(resourceName)!;
		using var reader = new StreamReader(stream);
		return reader.ReadToEnd();
	}

	private static string BuildSummaryStats(MergeReportSummary summary)
	{
		var sb = new StringBuilder();

		AppendStat(sb, "Sources", $"{summary.LoadedSources}/{summary.TotalSources} loaded");

		if (summary.FailedSources.HasValue)
			AppendStat(sb, "Failed", summary.FailedSources.Value.ToString(), "stat-warn");

		if (summary.TotalPathsMerged.HasValue)
			AppendStat(sb, "Paths Merged", summary.TotalPathsMerged.Value.ToString());

		if (summary.SchemaConflictsResolved.HasValue)
			AppendStat(sb, "Schema Conflicts", summary.SchemaConflictsResolved.Value.ToString(), "stat-info");

		if (summary.SecuritySchemeConflictsResolved.HasValue)
			AppendStat(sb, "Security Conflicts", summary.SecuritySchemeConflictsResolved.Value.ToString(), "stat-info");

		if (summary.SecuritySchemesDeduplicated.HasValue)
			AppendStat(sb, "Schemes Deduplicated", summary.SecuritySchemesDeduplicated.Value.ToString());

		if (summary.PathsExcluded.HasValue)
			AppendStat(sb, "Paths Excluded", summary.PathsExcluded.Value.ToString());

		if (summary.PathsSkipped.HasValue)
			AppendStat(sb, "Paths Skipped", summary.PathsSkipped.Value.ToString());

		return sb.ToString();
	}

	private static void AppendStat(StringBuilder sb, string label, string value, string? cssClass = null)
	{
		string cls = cssClass is not null ? $" {cssClass}" : "";
		sb.AppendLine($"<div class=\"stat{cls}\"><span class=\"stat-value\">{Escape(value)}</span><span class=\"stat-label\">{Escape(label)}</span></div>");
	}

	private static string BuildSourceRows(List<MergeReportSource> sources)
	{
		var sb = new StringBuilder();

		foreach (var source in sources)
		{
			string status = source.IsLoaded
				? "<span class=\"badge ok\">Loaded</span>"
				: $"<span class=\"badge fail\">Failed</span> <small>{Escape(source.ErrorMessage ?? "")}</small>";

			string virtualPrefix = source.VirtualPrefix is not null ? Escape(source.VirtualPrefix) : "&mdash;";
			string tagPrefix = source.PrefixTagsWith is not null ? Escape(source.PrefixTagsWith) : "&mdash;";
			sb.AppendLine(
				$"<tr><td><code>{Escape(source.Path)}</code></td>" +
				$"<td>{tagPrefix}</td>" +
				$"<td>{virtualPrefix}</td>" +
				$"<td>{status}</td></tr>");
		}

		return sb.ToString();
	}

	private static string BuildConflictsSection(MergeReportConflicts? conflicts)
	{
		if (conflicts is null)
			return "";

		var sb = new StringBuilder();
		sb.AppendLine("<section>");
		sb.AppendLine("<h2>Conflicts Resolved</h2>");

		if (conflicts.Schemas is { Count: > 0 })
		{
			sb.AppendLine("<h3>Schema Conflicts</h3>");
			sb.AppendLine("<table><thead>" +
				"<tr><th>Original Key</th><th>New Key</th>" +
				"<th>Resolution</th><th>Source</th>" +
				"</tr></thead><tbody>");
			foreach (var c in conflicts.Schemas)
			{
				sb.AppendLine($"<tr><td><code>{Escape(c.OriginalKey)}</code></td>" +
					$"<td><code>{Escape(c.NewKey)}</code></td>" +
					$"<td>{Escape(c.Resolution)}</td>" +
					$"<td>{Escape(c.SourceApi)}</td></tr>");
			}
			sb.AppendLine("</tbody></table>");
		}

		if (conflicts.SecuritySchemes is { Count: > 0 })
		{
			sb.AppendLine("<h3>Security Scheme Conflicts</h3>");
			sb.AppendLine("<table><thead>" +
				"<tr><th>Original Key</th><th>New Key</th>" +
				"<th>Resolution</th><th>Source</th>" +
				"</tr></thead><tbody>");
			foreach (var c in conflicts.SecuritySchemes)
			{
				sb.AppendLine($"<tr><td><code>{Escape(c.OriginalKey)}</code></td>" +
					$"<td><code>{Escape(c.NewKey)}</code></td>" +
					$"<td>{Escape(c.Resolution)}</td>" +
					$"<td>{Escape(c.SourceApi)}</td></tr>");
			}
			sb.AppendLine("</tbody></table>");
		}

		sb.AppendLine("</section>");
		return sb.ToString();
	}

	private static string BuildDeduplicationSection(MergeReportDeduplication? dedup)
	{
		if (dedup is null)
			return "";

		var sb = new StringBuilder();
		sb.AppendLine("<section>");
		sb.AppendLine("<h2>Deduplications</h2>");
		sb.AppendLine("<table><thead>" +
			"<tr><th>Security Scheme</th><th>Source API</th></tr></thead><tbody>");
		foreach (var d in dedup.SecuritySchemes)
			sb.AppendLine($"<tr><td><code>{Escape(d.Key)}</code></td><td>{Escape(d.SourceApi)}</td></tr>");
		sb.AppendLine("</tbody></table>");
		sb.AppendLine("</section>");
		return sb.ToString();
	}

	private static string BuildRemovalsSection(MergeReportRemovals? removals)
	{
		if (removals is null)
			return "";

		var sb = new StringBuilder();
		sb.AppendLine("<section>");
		sb.AppendLine("<h2>Removals</h2>");

		if (removals.ExcludedPaths is { Count: > 0 })
		{
			sb.AppendLine("<h3>Excluded Paths</h3>");
			sb.AppendLine("<table><thead><tr><th>Path</th><th>API</th><th>Pattern</th></tr></thead><tbody>");
			foreach (var e in removals.ExcludedPaths)
				sb.AppendLine($"<tr><td><code>{Escape(e.Path)}</code></td><td>{Escape(e.Api)}</td><td><code>{Escape(e.Pattern)}</code></td></tr>");
			sb.AppendLine("</tbody></table>");
		}

		if (removals.SkippedPaths is { Count: > 0 })
		{
			sb.AppendLine("<h3>Skipped Paths (Duplicates)</h3>");
			sb.AppendLine("<table><thead><tr><th>Path</th><th>API</th></tr></thead><tbody>");
			foreach (var s in removals.SkippedPaths)
				sb.AppendLine($"<tr><td><code>{Escape(s.Path)}</code></td><td>{Escape(s.Api)}</td></tr>");
			sb.AppendLine("</tbody></table>");
		}

		sb.AppendLine("</section>");
		return sb.ToString();
	}

	private static string Escape(string text) =>
		System.Net.WebUtility.HtmlEncode(text);
}
