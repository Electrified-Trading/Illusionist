namespace Illusionist.Core;

/// <summary>
/// Factory for creating <see cref="DefaultEquitiesSchedule"/> instances.
/// Returns a consistent equity market schedule regardless of the bar interval.
/// </summary>
public sealed class DefaultEquitiesScheduleFactory : IScheduleFactory
{
    /// <summary>
    /// Returns a market schedule for the specified bar interval.
    /// All intervals use the same equity market trading hours and holiday schedule.
    /// </summary>
    /// <param name="interval">The bar interval for which to create the market schedule.</param>
    /// <returns>A <see cref="DefaultEquitiesSchedule"/> configured for the specified interval.</returns>
    public ISchedule GetSchedule(BarInterval interval)
    {
        return new DefaultEquitiesSchedule(interval);
    }
}
