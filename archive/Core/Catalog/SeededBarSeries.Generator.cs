namespace Illusionist.Core.Catalog;

public sealed partial class SeededBarSeries
{
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
		{			// Align timestamp to interval boundary
			var alignedTimestamp = AlignToInterval(timestamp);

			// Convert to seconds since Unix epoch for mathematical operations
			var t = (alignedTimestamp - DateTime.UnixEpoch).TotalSeconds;			// Layer two sine waves for realistic price movement
			var baseWave = Math.Sin(seed + t * 0.0001);
			var harmonic = Math.Sin(seed * 0.5 + t * 0.0003);			// Add spike variation using hash-based deterministic "randomness"
			var hash = CreateDeterministicHash(alignedTimestamp, seed);
			var spike = (hash % 5) / 8.0;

			// Derive OHLCV values with realistic relationships
			var open = baseWave * 10 + harmonic * 2 + 100; // Base price around 100
			var high = open + Math.Abs(harmonic) + spike;
			var low = open - Math.Abs(baseWave) - spike;
			var close = open + Math.Sin(t * 0.00005 + seed);
			var volume = Math.Abs(hash % 10000) + 1000; // Volume between 1000-11000// Ensure high >= low and both contain open/close
			var actualHigh = Math.Max(Math.Max(open, close), high);
			var actualLow = Math.Min(Math.Min(open, close), low);			return new Bar(
				Timestamp: alignedTimestamp, // Return the aligned timestamp for interval boundary consistency
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
		{
			// Use a simple but deterministic hash algorithm
			var timestampHash = (int)(timestamp.Ticks ^ (timestamp.Ticks >> 32));
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
