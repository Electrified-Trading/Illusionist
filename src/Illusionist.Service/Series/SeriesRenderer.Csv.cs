using System.Globalization;
using System.Text;

namespace Illusionist.Service.Series;

public static partial class SeriesRenderer
{
	private const string CsvHeader = "Index,TimestampTicks,Open,High,Low,Close,Volume";

	/// <summary>
	/// <c>GoldenBarFormatter</c>'s exact layout: <see cref="CsvHeader"/>, one row per bar, invariant
	/// culture, LF line ends, a single trailing newline. Index is 0-based within this response.
	/// </summary>
	private static string RenderCsv(IReadOnlyList<Bar<OHLC>> bars)
	{
		var builder = new StringBuilder(CsvHeader.Length + 1 + bars.Count * 96);
		builder.Append(CsvHeader);

		for (var i = 0; i < bars.Count; i++)
		{
			var bar = bars[i];
			builder.Append('\n');
			builder.Append(i.ToString(CultureInfo.InvariantCulture)).Append(',');
			builder.Append(bar.Timestamp.Ticks.ToString(CultureInfo.InvariantCulture)).Append(',');
			builder.Append(bar.Data.Open.ToString(CultureInfo.InvariantCulture)).Append(',');
			builder.Append(bar.Data.High.ToString(CultureInfo.InvariantCulture)).Append(',');
			builder.Append(bar.Data.Low.ToString(CultureInfo.InvariantCulture)).Append(',');
			builder.Append(bar.Data.Close.ToString(CultureInfo.InvariantCulture)).Append(',');
			builder.Append(bar.Volume.ToString(CultureInfo.InvariantCulture));
		}

		builder.Append('\n');
		return builder.ToString();
	}
}
