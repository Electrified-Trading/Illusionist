using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Illusionist.Service.Series;

public static partial class SeriesRenderer
{
	/// <summary>
	/// <c>{"uri":…,"columns":[…],"rows":[[timestamp,open,high,low,close,volume]…]}</c>. Prices are
	/// written raw (<see cref="Utf8JsonWriter.WriteRawValue(string,bool)"/>) so they carry the exact
	/// same digits as <see cref="RenderCsv"/>. Timestamps are exchange-local, no offset.
	/// </summary>
	private static string RenderJson(SeriesKey key, IReadOnlyList<Bar<OHLC>> bars)
	{
		using var stream = new MemoryStream();
		using (var writer = new Utf8JsonWriter(stream, IllusionistJson.WriterOptions))
		{
			writer.WriteStartObject();
			writer.WriteString("uri", SeriesQuery.ResourceUri(key));

			writer.WriteStartArray("columns");
			writer.WriteStringValue("timestamp");
			writer.WriteStringValue("open");
			writer.WriteStringValue("high");
			writer.WriteStringValue("low");
			writer.WriteStringValue("close");
			writer.WriteStringValue("volume");
			writer.WriteEndArray();

			writer.WriteStartArray("rows");
			foreach (var bar in bars)
			{
				writer.WriteStartArray();
				writer.WriteStringValue(bar.Timestamp.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture));
				writer.WriteRawValue(bar.Data.Open.ToString(CultureInfo.InvariantCulture), skipInputValidation: true);
				writer.WriteRawValue(bar.Data.High.ToString(CultureInfo.InvariantCulture), skipInputValidation: true);
				writer.WriteRawValue(bar.Data.Low.ToString(CultureInfo.InvariantCulture), skipInputValidation: true);
				writer.WriteRawValue(bar.Data.Close.ToString(CultureInfo.InvariantCulture), skipInputValidation: true);
				writer.WriteNumberValue(bar.Volume);
				writer.WriteEndArray();
			}

			writer.WriteEndArray();
			writer.WriteEndObject();
		}

		// Both wire layouts end with a single LF (D3) -- Utf8JsonWriter emits none on its own.
		return Encoding.UTF8.GetString(stream.ToArray()) + "\n";
	}
}
