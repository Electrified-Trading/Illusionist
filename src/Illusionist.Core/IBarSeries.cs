namespace Illusionist.Core;

/// <summary>
/// Provides access to bar data for a specific time series.
/// Supports both point-in-time queries and range-based streaming.
/// </summary>
public interface IBarSeries
{
	/// <summary>
	/// Gets the bar that contains or immediately precedes the specified timestamp.
	/// </summary>
	/// <param name="timestamp">The timestamp to query</param>
	/// <returns>The bar for the specified timestamp</returns>
	Bar GetBarAt(DateTime timestamp);

	/// <summary>
	/// Gets an enumerable sequence of bars starting from the specified timestamp.
	/// </summary>
	/// <param name="start">The starting timestamp</param>
	/// <returns>An enumerable sequence of bars</returns>
	IEnumerable<Bar> GetBars(DateTime start);
}
