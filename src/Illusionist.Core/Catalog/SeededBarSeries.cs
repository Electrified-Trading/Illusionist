namespace Illusionist.Core.Catalog;

/// <summary>
/// A deterministic bar series implementation that uses SeededBarSeries.Generator
/// to provide reproducible OHLCV data for any time interval.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="SeededBarSeries"/> class.
/// </remarks>
/// <param name="seed">The random seed for deterministic generation</param>
/// <param name="interval">The time interval between bars</param>
public sealed class SeededBarSeries(int seed, TimeSpan interval) : IBarSeries
{
	private readonly Generator _generator = new(seed, interval);

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

	/// <summary>
	/// Generates deterministic OHLCV bars using a seeded algorithm.
	/// Produces realistic-looking synthetic market data that is reproducible across runs.
	/// </summary>
	/// <remarks>
	/// Initializes a new instance of the <see cref="Generator"/> class.
	/// </remarks>
	/// <param name="seed">The random seed for deterministic generation</param>
	/// <param name="interval">The time interval between bars</param>
	public sealed class Generator(int seed, TimeSpan interval)
	{
		/// <summary>
		/// Gets the time interval between bars.
		/// </summary>
		public TimeSpan Interval => interval;

		/// <summary>
		/// Gets the deterministic bar for the specified timestamp.
		/// The timestamp will be aligned to the bar interval boundary.
		/// </summary>
		/// <param name="timestamp">The timestamp to generate a bar for</param>
		/// <returns>A deterministic bar with OHLCV data</returns>
		public Bar GetBarAt(DateTime timestamp)
		{
			// Align timestamp to interval boundary
			var alignedTimestamp = AlignToInterval(timestamp);

			// Convert to seconds since Unix epoch for mathematical operations
			var t = (alignedTimestamp - DateTime.UnixEpoch).TotalSeconds;

			// Layer two sine waves for realistic price movement
			var baseWave = Math.Sin(seed + t * 0.0001);
			var harmonic = Math.Sin(seed * 0.5 + t * 0.0003);
			// Add spike variation using hash-based deterministic "randomness"
			var hash = CreateDeterministicHash(alignedTimestamp, seed);
			var spike = hash % 5 / 5.0;

			// Derive OHLCV values with realistic relationships
			var open = baseWave * 10 + harmonic * 2 + 100; // Base price around 100
			var high = open + Math.Abs(harmonic) + spike;
			var low = open - Math.Abs(baseWave) - spike;
			var close = open + Math.Sin(t * 0.00005 + seed);
			var volume = Math.Abs(hash % 10000) + 1000; // Volume between 1000-11000

			// Ensure high >= low and both contain open/close
			var actualHigh = Math.Max(Math.Max(open, close), high);
			var actualLow = Math.Min(Math.Min(open, close), low);

			return new Bar(
				Timestamp: alignedTimestamp,
				Open: (decimal)open,
				High: (decimal)actualHigh,
				Low: (decimal)actualLow,
				Close: (decimal)close,
				Volume: volume);
		}

		/// <summary>
		/// Creates a deterministic hash value from timestamp and seed.
		/// Uses a simple but effective hash algorithm that's guaranteed to be consistent.
		/// </summary>
		/// <param name="timestamp">The timestamp to hash</param>
		/// <param name="seed">The seed value</param>
		/// <returns>A deterministic hash value</returns>
		private static int CreateDeterministicHash(DateTime timestamp, int seed)
		{        // Use a simple but deterministic hash algorithm
			var timestampHash = (int)(timestamp.Ticks ^ timestamp.Ticks >> 32);
			var combined = timestampHash ^ seed;
			// Apply additional mixing to improve distribution
			unchecked
			{
				combined ^= combined >> 16;
				combined *= (int)0x85ebca6b;
				combined ^= combined >> 13;
				combined *= (int)0xc2b2ae35;
				combined ^= combined >> 16;
			}

			return Math.Abs(combined);
		}

		/// <summary>
		/// Aligns the given timestamp to the nearest interval boundary.
		/// </summary>
		/// <param name="timestamp">The timestamp to align</param>
		/// <returns>The aligned timestamp</returns>
		private DateTime AlignToInterval(DateTime timestamp)
		{
			var ticks = timestamp.Ticks;
			var intervalTicks = interval.Ticks;
			var alignedTicks = ticks / intervalTicks * intervalTicks;
			return new DateTime(alignedTicks, timestamp.Kind);
		}
	}
}
