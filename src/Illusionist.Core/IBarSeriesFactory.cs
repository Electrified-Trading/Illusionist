namespace Illusionist.Core;

/// <summary>
/// Factory for creating deterministic bar series instances.
/// Configured with a seed to ensure reproducible data generation.
/// </summary>
public interface IBarSeriesFactory
{
	/// <summary>
	/// Creates a bar series with the specified bar interval and anchor point.
	/// </summary>
	/// <param name="schedule">The schedule defining valid bars.</param>
	/// <param name="anchor">The anchor point for bar alignment and pricing reference</param>
	/// <returns>A deterministic bar series instance</returns>
	IBarSeries GetSeries(ISchedule schedule, BarAnchor anchor);
}
