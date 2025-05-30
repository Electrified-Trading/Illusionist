namespace Illusionist.Core;

/// <summary>
/// Defines a factory for creating <see cref="ISchedule"/> instances based on a specified <see cref="BarInterval"/>.
/// </summary>
public interface IScheduleFactory
{
    /// <summary>
    /// Returns a market schedule for the specified bar interval.
    /// </summary>
    /// <param name="interval">The bar interval for which to create the market schedule.</param>
    ISchedule GetSchedule(BarInterval interval);
}