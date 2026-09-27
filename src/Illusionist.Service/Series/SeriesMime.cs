namespace Illusionist.Service.Series;

/// <summary>The MIME type each <see cref="SeriesFormat"/> renders as, shared by REST and the MCP resource surface.</summary>
public static class SeriesMime
{
	/// <summary>Returns <c>text/csv</c> or <c>application/json</c> for <paramref name="format"/>.</summary>
	public static string For(SeriesFormat format)
		=> format switch
		{
			SeriesFormat.Csv => "text/csv",
			SeriesFormat.Json => "application/json",
			_ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unhandled series format."),
		};
}
