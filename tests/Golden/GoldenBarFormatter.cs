using System.Globalization;

namespace Illusionist.Tests.Golden;

/// <summary>
/// Formats a bar sequence into the exact byte layout the committed fixture files under
/// <c>Golden/Fixtures/</c> use -- a header row followed by one CSV row per bar, LF-terminated
/// (never <see cref="Environment.NewLine"/>, so the comparison in
/// <see cref="GeneratorGoldenTests"/> is not itself platform-sensitive) and always ending in a
/// trailing newline.
/// </summary>
/// <remarks>
/// Timestamps are written as raw <see cref="DateTime.Ticks"/> rather than a formatted date --
/// the same reasoning a downstream consumer's own chart-spec type gives for canonicalizing its
/// own anchor by ticks: ticks are <see cref="DateTimeKind"/>-independent, so this format can
/// never be ambiguous about a timezone/offset basis it does not otherwise carry.
/// </remarks>
internal static class GoldenBarFormatter
{
	private const string Header = "Index,TimestampTicks,Open,High,Low,Close,Volume";

	/// <summary>Formats <paramref name="bars"/> into the fixture file's exact byte layout.</summary>
	public static string Format(IReadOnlyList<Bar<OHLC>> bars)
	{
		var lines = new List<string>(bars.Count + 1) { Header };

		for (var i = 0; i < bars.Count; i++)
		{
			var bar = bars[i];
			lines.Add(string.Join(
				',',
				i.ToString(CultureInfo.InvariantCulture),
				bar.Timestamp.Ticks.ToString(CultureInfo.InvariantCulture),
				bar.Data.Open.ToString(CultureInfo.InvariantCulture),
				bar.Data.High.ToString(CultureInfo.InvariantCulture),
				bar.Data.Low.ToString(CultureInfo.InvariantCulture),
				bar.Data.Close.ToString(CultureInfo.InvariantCulture),
				bar.Volume.ToString(CultureInfo.InvariantCulture)));
		}

		return string.Join('\n', lines) + "\n";
	}
}
