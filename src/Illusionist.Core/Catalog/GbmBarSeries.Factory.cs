namespace Illusionist.Core.Catalog;

public sealed partial class GbmBarSeries
{	/// <summary>
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
	{		/// <summary>
		/// Creates a GBM bar series with the specified bar interval and anchor point.
		/// </summary>
		/// <param name="interval">The bar interval configuration (unit and length)</param>
		/// <param name="anchor">The anchor point for bar alignment and pricing reference</param>
		/// <returns>A deterministic GBM bar series instance</returns>
		public IBarSeries GetSeries(BarInterval interval, BarAnchor anchor)
		{
			return new GbmBarSeries(symbol, seed, interval.Interval, anchor, drift, volatility);
		}
	}
}
