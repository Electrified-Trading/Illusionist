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
/// <param name="interval">The time interval between bars</param>
/// <param name="drift">The drift parameter for GBM (default: 0.0001)</param>
/// <param name="volatility">The volatility parameter for GBM (default: 0.01)</param>
public sealed partial class GbmBarSeries(
    string symbol, 
    int seed, 
    TimeSpan interval, 
    double drift = 0.0001, 
    double volatility = 0.01) : IBarSeries
{
    private readonly Generator _generator = new(symbol, seed, interval, drift, volatility);

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
    /// Each bar follows GBM price evolution from the previous bar.
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
