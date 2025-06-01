namespace Illusionist.Core.Catalog;

/// <summary>
/// A deterministic bar series implementation that uses Geometric Brownian Motion (GBM)
/// to provide realistic price evolution with log-normal growth characteristics.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GbmBarSeries"/> class.
/// </remarks>
/// <param name="symbol">The trading symbol for this series</param>
/// <param name="seed">The random seed for deterministic generation</param>
/// <param name="schedule">The schedule defining valid bars</param>
/// <param name="anchor">The anchor point for time and price reference</param>
/// <param name="drift">The drift parameter for GBM (default: 0.0001)</param>
/// <param name="volatility">The volatility parameter for GBM (default: 0.01)</param>
public sealed partial class GbmBarSeries(
	string symbol,
	int seed,
	ISchedule schedule,
	BarAnchor anchor,
	double drift = 0.0001,
	double volatility = 0.01) : IBarSeries
{
	private readonly Generator _generator = new(seed + symbol.GetHashCode(), schedule, drift, volatility, anchor);

	/// <summary>
	/// Gets the bar that contains or immediately precedes the specified timestamp.
	/// Uses Geometric Brownian Motion to generate realistic price evolution.
	/// </summary>
	/// <param name="timestamp">The timestamp to query</param>
	/// <returns>The bar for the specified timestamp</returns>
	public Bar GetBarAt(DateTime timestamp)
	{
		return _generator.GetBarAt(timestamp);
	}
	/// <summary>
	/// Gets an enumerable sequence of bars starting from the specified timestamp.
	/// Each bar follows GBM price evolution and uses schedule-aware time advancement.
	/// </summary>
	/// <param name="start">The starting timestamp</param>
	/// <returns>An enumerable sequence of bars</returns>
	public IEnumerable<Bar> GetBars(DateTime start)
	{
		var current = start;
		var firstBar = _generator.GetBarAt(current);
		yield return firstBar;
		
		// Use schedule-aware time advancement
		while (true)
		{
			current = _generator.Schedule.GetNextValidBarTime(current);
			yield return _generator.GetBarAt(current);
		}
	}
}
