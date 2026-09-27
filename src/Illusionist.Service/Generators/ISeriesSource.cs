namespace Illusionist.Service.Generators;

/// <summary>
/// A generator version opened against one <see cref="Illusionist.Service.Contracts.SeriesKey"/>:
/// stateless random access to any bar the key's extent covers.
/// </summary>
public interface ISeriesSource
{
	/// <summary>
	/// The timestamps this source's extent covers, in order, or <see langword="null"/> when there
	/// are more than <paramref name="maxBars"/> of them. Range extents are walked against the
	/// schedule to compute this (holidays and weekends are not arithmetic); count extents never
	/// exceed <paramref name="maxBars"/> by construction.
	/// </summary>
	IReadOnlyList<DateTime>? Timestamps(int maxBars);

	/// <summary>Generates the bar at exactly <paramref name="timestamp"/>. Pure: no memory of prior calls.</summary>
	Bar<OHLC> BarAt(DateTime timestamp);
}
