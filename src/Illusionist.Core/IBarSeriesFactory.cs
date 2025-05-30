namespace Illusionist.Core;

/// <summary>
/// Factory for creating deterministic bar series instances.
/// Configured with a seed to ensure reproducible data generation.
/// </summary>
public interface IBarSeriesFactory
{
	/// <summary>
	/// Creates a bar series with the specified time interval.
	/// </summary>
	/// <param name="interval">The time interval between bars (e.g., 1 minute, 5 minutes, 1 hour)</param>
	/// <returns>A deterministic bar series instance</returns>
	IBarSeries GetSeries(TimeSpan interval);
}
