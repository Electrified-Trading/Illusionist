using Illusionist.Core.Generators;

namespace Illusionist.Core;

/// <summary>
/// A deterministic bar series implementation that uses SeededBarGenerator
/// to provide reproducible OHLCV data for any time interval.
/// </summary>
public sealed class SeededBarSeries : IBarSeries
{
    private readonly SeededBarGenerator _generator;

    /// <summary>
    /// Initializes a new instance of the <see cref="SeededBarSeries"/> class.
    /// </summary>
    /// <param name="seed">The random seed for deterministic generation</param>
    /// <param name="interval">The time interval between bars</param>
    public SeededBarSeries(int seed, TimeSpan interval)
    {
        _generator = new SeededBarGenerator(seed, interval);
    }

    /// <summary>
    /// Gets the bar that contains or immediately precedes the specified timestamp.
    /// </summary>
    /// <param name="timestamp">The timestamp to query</param>
    /// <returns>The bar for the specified timestamp</returns>
    public Bar GetBarAt(DateTime timestamp)
    {
        return _generator.GetBarAt(timestamp);
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
