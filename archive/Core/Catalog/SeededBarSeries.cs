namespace Illusionist.Core.Catalog;

/// <summary>
/// A deterministic bar series implementation that uses SeededBarSeries.Generator
/// to provide reproducible OHLCV data for any time interval.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="SeededBarSeries"/> class.
/// </remarks>
/// <param name="seed">The random seed for deterministic generation</param>
/// <param name="interval">The time interval between bars</param>
public sealed partial class SeededBarSeries(int seed, TimeSpan interval) : IBarSeries
{
	private readonly Generator _generator = new(seed, interval);
	/// <summary>
	/// Gets the bar that contains or immediately precedes the specified timestamp.
	/// </summary>
	/// <param name="timestamp">The timestamp to query</param>
	/// <returns>The bar for the specified timestamp</returns>
	public Bar GetBarAt(DateTime timestamp)
	{
		var bar = _generator.GetBarAt(timestamp);
		// Return the bar with the original requested timestamp to match IBarSeries contract
		return bar with { Timestamp = timestamp };
	}

	/// <summary>
	/// Gets an enumerable sequence of bars starting from the specified timestamp.
	/// </summary>
	/// <param name="start">The starting timestamp</param>
	/// <returns>An enumerable sequence of bars</returns>
	public IEnumerable<Bar> GetBars(DateTime start)
	{
		var current = start;

		while (true)
		{
			yield return _generator.GetBarAt(current);
			current = current.Add(_generator.Interval);
		}
	}
}
