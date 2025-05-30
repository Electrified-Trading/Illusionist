namespace Illusionist.Core.Catalog;

public sealed partial class SeededBarSeries
{
	/// <summary>
	/// Factory for creating deterministic bar series instances using a seeded generator.
	/// Configured with a seed to ensure reproducible data generation across multiple series.
	/// </summary>
	/// <remarks>
	/// Initializes a new instance of the <see cref="Factory"/> class.
	/// </remarks>
	/// <param name="seed">The random seed for deterministic generation</param>
	public sealed class Factory(int seed) : IBarSeriesFactory
	{
		/// <summary>
		/// Creates a bar series with the specified time interval.
		/// </summary>
		/// <param name="interval">The time interval between bars (e.g., 1 minute, 5 minutes, 1 hour)</param>
		/// <returns>A deterministic bar series instance</returns>
		public IBarSeries GetSeries(TimeSpan interval)
		{
			return new SeededBarSeries(seed, interval);
		}
	}
}
