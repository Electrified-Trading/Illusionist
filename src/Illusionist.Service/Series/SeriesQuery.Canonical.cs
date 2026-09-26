using System.Globalization;

namespace Illusionist.Service.Series;

public static partial class SeriesQuery
{
	/// <summary>
	/// Renders <paramref name="key"/> as its one canonical query string: fixed field order, parts
	/// that don't apply left out, every value passed through <see cref="Uri.EscapeDataString"/>.
	/// Round-trips through <see cref="ParseQueryText"/> and <c>SeriesKeyParser.Parse</c> to the
	/// identical string, regardless of the spelling originally received.
	/// </summary>
	public static string Canonical(SeriesKey key)
	{
		var parts = new List<string>(11)
		{
			Pair("generator", key.Generator.ToString()),
			Pair("seedMode", key.SeedMode == SeedMode.Bare ? "bare" : "symbol-hashed"),
			Pair("seed", key.Seed.ToString(CultureInfo.InvariantCulture)),
		};

		if (key.Symbol is not null)
			parts.Add(Pair("symbol", key.Symbol));

		parts.Add(Pair("drift", FormatDouble(key.Drift)));
		parts.Add(Pair("volatility", FormatDouble(key.Volatility)));
		parts.Add(Pair("anchorDate", FormatDate(key.AnchorDate)));
		parts.Add(Pair("anchorPrice", key.AnchorPrice.ToString(CultureInfo.InvariantCulture)));
		parts.Add(Pair("timeframe", key.Timeframe));

		switch (key.Extent)
		{
			case SeriesExtent.Count(var count):
				parts.Add(Pair("count", count.ToString(CultureInfo.InvariantCulture)));
				break;

			case SeriesExtent.Range(var from, var to):
				parts.Add(Pair("from", FormatDate(from)));
				parts.Add(Pair("to", FormatDate(to)));
				break;
		}

		parts.Add(Pair("format", key.Format == SeriesFormat.Csv ? "csv" : "json"));

		return string.Join('&', parts);
	}

	/// <summary>The resource URI: <c>illusionist://series?</c> plus the canonical query.</summary>
	public static string ResourceUri(SeriesKey key)
		=> string.Concat("illusionist://series?", Canonical(key));

	/// <summary>
	/// The REST path: <c>/v1/series?</c> plus the canonical query, made absolute when
	/// <paramref name="publicBaseUrl"/> is set.
	/// </summary>
	public static string RestPath(SeriesKey key, string? publicBaseUrl)
		=> string.Concat(publicBaseUrl, "/v1/series?", Canonical(key));

	private static string Pair(string name, string value)
		=> string.Concat(name, "=", Uri.EscapeDataString(value));

	private static string FormatDate(DateOnly date)
		=> date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

	/// <summary>
	/// <c>double.ToString("R")</c>, except exact zero (including negative zero) always prints as
	/// <c>0</c> -- two byte-identical geometries must not produce two different cache keys.
	/// </summary>
	private static string FormatDouble(double value)
		=> value == 0
			? "0"
			: value.ToString("R", CultureInfo.InvariantCulture);
}
