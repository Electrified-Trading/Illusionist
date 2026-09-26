using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Illusionist.Service.Series;

/// <summary>The first open, max High, min Low and last Close of a rendered series -- <see langword="null"/> when there are 0 bars.</summary>
public sealed record OhlcSummary(decimal Open, decimal High, decimal Low, decimal Close);

/// <summary>One fully generated and rendered series: the bytes every surface serves, plus the metadata the envelope reports.</summary>
public sealed record SeriesResult(
	SeriesKey Key,
	string Body,
	int ByteCount,
	int BarCount,
	DateTime? First,
	DateTime? Last,
	OhlcSummary? Summary,
	IReadOnlyList<string> Warnings)
{
	/// <summary>Whether <see cref="Body"/> is small enough to come back inline (D3's fixed threshold).</summary>
	public bool Inline => ByteCount <= ServiceLimits.InlineMaxBytes;
}

/// <summary>
/// Builds <c>illusionist_series</c>'s one-line JSON envelope (D3): the small, stable summary every
/// MCP call gets in <c>content[0]</c>, regardless of whether the body itself comes back inline or
/// as a resource link.
/// </summary>
public static class SeriesEnvelope
{
	/// <summary>The one calendar-scope warning text, emitted whenever any bar falls outside 2024-2025.</summary>
	public const string CalendarScopeWarning =
		"calendar: U.S. market holidays are only known for 2024-2025; outside those years they are treated as trading days.";

	/// <summary>
	/// A well-formed, in-range key (legal drift/volatility/anchorPrice/date bounds) can still walk
	/// the price arbitrarily far from <c>anchorPrice</c> without overflowing -- this is a
	/// plausibility signal, not a validation failure: the key succeeds and the bytes are unchanged,
	/// same channel as <see cref="CalendarScopeWarning"/>, non-breaking by construction.
	/// </summary>
	/// <param name="ratio">The extreme/anchor price ratio (always &gt;= 1), formatted for the message.</param>
	/// <param name="aboveAnchor"><see langword="true"/> for the highest-high case, <see langword="false"/> for the lowest-low case.</param>
	public static string ExtremePricesWarning(decimal ratio, bool aboveAnchor)
	{
		var formattedRatio = ratio.ToString("0.##", CultureInfo.InvariantCulture);
		return aboveAnchor
			? $"extreme_prices: the series' highest high is {formattedRatio}x the anchor price."
			: $"extreme_prices: the series' lowest low is 1/{formattedRatio}x the anchor price.";
	}

	/// <summary>Builds the envelope JSON for <paramref name="result"/>.</summary>
	public static string Build(SeriesResult result, string? publicBaseUrl)
	{
		using var stream = new MemoryStream();
		using (var writer = new Utf8JsonWriter(stream, IllusionistJson.WriterOptions))
		{
			writer.WriteStartObject();
			writer.WriteString("uri", SeriesQuery.ResourceUri(result.Key));
			writer.WriteString("rest", SeriesQuery.RestPath(result.Key, publicBaseUrl));
			writer.WriteString("format", result.Key.Format == SeriesFormat.Csv ? "csv" : "json");
			writer.WriteNumber("bars", result.BarCount);
			writer.WriteNumber("bytes", result.ByteCount);
			writer.WriteBoolean("inline", result.Inline);
			WriteTimestampOrNull(writer, "first", result.First);
			WriteTimestampOrNull(writer, "last", result.Last);
			WriteSummaryOrNull(writer, result.Summary);

			writer.WriteStartArray("warnings");
			foreach (var warning in result.Warnings)
				writer.WriteStringValue(warning);
			writer.WriteEndArray();

			writer.WriteEndObject();
		}

		return Encoding.UTF8.GetString(stream.ToArray());
	}

	private static void WriteTimestampOrNull(Utf8JsonWriter writer, string name, DateTime? value)
	{
		if (value is { } timestamp)
			writer.WriteString(name, timestamp.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture));
		else
			writer.WriteNull(name);
	}

	private static void WriteSummaryOrNull(Utf8JsonWriter writer, OhlcSummary? summary)
	{
		if (summary is null)
		{
			writer.WriteNull("summary");
			return;
		}

		writer.WriteStartObject("summary");
		WriteRawDecimal(writer, "open", summary.Open);
		WriteRawDecimal(writer, "high", summary.High);
		WriteRawDecimal(writer, "low", summary.Low);
		WriteRawDecimal(writer, "close", summary.Close);
		writer.WriteEndObject();
	}

	private static void WriteRawDecimal(Utf8JsonWriter writer, string name, decimal value)
	{
		writer.WritePropertyName(name);
		writer.WriteRawValue(value.ToString(CultureInfo.InvariantCulture), skipInputValidation: true);
	}
}
