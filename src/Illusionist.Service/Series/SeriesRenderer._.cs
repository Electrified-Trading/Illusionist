namespace Illusionist.Service.Series;

/// <summary>
/// Renders a bar sequence into one of the two frozen wire layouts (D3). Both are frozen with
/// service v1: a layout change means a new <see cref="SeriesFormat"/> name, never an edit to an
/// existing one. Both always end in a single LF.
/// </summary>
public static partial class SeriesRenderer
{
	/// <summary>Renders <paramref name="bars"/> under <paramref name="key"/>'s format.</summary>
	public static string Render(SeriesKey key, IReadOnlyList<Bar<OHLC>> bars)
		=> key.Format switch
		{
			SeriesFormat.Csv => RenderCsv(bars),
			SeriesFormat.Json => RenderJson(key, bars),
			_ => throw new ArgumentOutOfRangeException(nameof(key), key.Format, "Unhandled series format."),
		};
}
