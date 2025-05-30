using Illusionist.Core.Catalog;

namespace Illusionist.Core;

/// <summary>
/// Factory for creating GBM-based bar series instances with deterministic behavior.
/// Uses Geometric Brownian Motion to simulate realistic market price evolution.
/// </summary>
public sealed class GbmBarSeriesFactory : IBarSeriesFactory
{
    private readonly GbmBarSeries.Factory _factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="GbmBarSeriesFactory"/> class.
    /// </summary>
    /// <param name="symbol">The trading symbol for generated series</param>
    /// <param name="seed">The random seed for deterministic generation</param>
    /// <param name="drift">The drift parameter for GBM (annual growth rate, default: 0.0001)</param>
    /// <param name="volatility">The volatility parameter for GBM (annual volatility, default: 0.01)</param>
    public GbmBarSeriesFactory(string symbol, int seed, double drift = 0.0001, double volatility = 0.01)
    {
        _factory = new GbmBarSeries.Factory(symbol, seed, drift, volatility);
    }

    /// <summary>
    /// Creates a GBM bar series with the specified time interval.
    /// </summary>
    /// <param name="interval">The time interval between bars (e.g., 1 minute, 5 minutes, 1 hour)</param>
    /// <returns>A deterministic GBM bar series instance</returns>
    public IBarSeries GetSeries(TimeSpan interval)
    {
        return _factory.GetSeries(interval);
    }
}
