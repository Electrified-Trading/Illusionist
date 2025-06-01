using Illusionist.Core;

namespace Illusionist.Models;

/// <summary>
/// Factory for creating deterministic bar series instances.
/// Configured with a seed to ensure reproducible data generation.
/// </summary>
public interface IBarSeriesFactory
{    /// <summary>
    /// Creates a bar series with the specified schedule and anchor point.
    /// </summary>
    /// <param name="schedule">The schedule configuration for valid trading times</param>
    /// <param name="anchor">The anchor point for bar alignment and pricing reference</param>
    /// <returns>A deterministic bar series instance</returns>
    IBarSeries GetSeries(ISchedule schedule, BarAnchor anchor);
}
