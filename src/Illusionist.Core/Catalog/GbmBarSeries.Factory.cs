namespace Illusionist.Core.Catalog;

public sealed partial class GbmBarSeries
{
	/// <summary>
	/// Factory for creating GBM-based bar series instances with deterministic behavior.
	/// Uses Geometric Brownian Motion to simulate realistic market price evolution.
	/// </summary>
	/// <remarks>
	/// Initializes a new instance of the <see cref="Factory"/> class.
	/// </remarks>
	/// <param name="symbol">The trading symbol for generated series</param>
	/// <param name="seed">The random seed for deterministic generation</param>
	/// <param name="drift">The drift parameter for GBM (annual growth rate)</param>
	/// <param name="volatility">The volatility parameter for GBM (annual volatility)</param>
	public sealed class Factory(string symbol, int seed, double drift = 0.0001, double volatility = 0.01) : IBarSeriesFactory
	{
		/// <summary>
		/// Creates a GBM bar series with the specified time interval.
		/// </summary>
		/// <param name="interval">The time interval between bars (e.g., 1 minute, 5 minutes, 1 hour)</param>
		/// <returns>A deterministic GBM bar series instance</returns>
		public IBarSeries GetSeries(TimeSpan interval)
		{
			return new GbmBarSeries(symbol, seed, interval, drift, volatility);
		}
	}
}
