namespace Illusionist.Core.Catalog;

public sealed partial class GbmBarSeries
{	/// <summary>
	/// Generator that implements Geometric Brownian Motion for deterministic price evolution.
	/// Uses log-normal distribution characteristics to create realistic market-like behavior.
	/// </summary>
	/// <remarks>
	/// Initializes a new instance of the <see cref="Generator"/> class.
	/// </remarks>
	/// <param name="seed">The random seed for deterministic generation</param>
	/// <param name="interval">The time interval between bars</param>
	/// <param name="drift">The drift parameter for GBM</param>
	/// <param name="volatility">The volatility parameter for GBM</param>
	/// <param name="anchor">The anchor point for time and price reference</param>
	public sealed class Generator(int seed, TimeSpan interval, double drift, double volatility, BarAnchor anchor)
	{
		private readonly int _intervalHash = interval.Ticks.GetHashCode();
		private readonly int _paramHash = (drift.GetHashCode() ^ volatility.GetHashCode());

		private static readonly DateTime Epoch = DateTime.UnixEpoch;

		// Scale parameters to work with hourly time units and make them more reasonable
		private readonly double _hourlyDrift = drift / (365.25 * 24); // Convert annual drift to hourly
		private readonly double _hourlyVolatility = volatility / Math.Sqrt(365.25 * 24); // Convert annual volatility to hourly

		/// <summary>
		/// Gets the time interval between bars.
		/// </summary>
		public TimeSpan Interval => interval;
		/// <summary>
		/// Generates a deterministic bar using Geometric Brownian Motion at the specified timestamp.
		/// The timestamp will be aligned to the bar interval boundary.
		/// </summary>
		/// <param name="timestamp">The timestamp to generate a bar for</param>
		/// <returns>A deterministic bar with GBM-based OHLCV data</returns>
		public Bar GetBarAt(DateTime timestamp)
		{
			// Align timestamp to interval boundary
			var alignedTimestamp = AlignToInterval(timestamp);

			// Compute time delta from anchor in seconds for GBM calculation
			var t = (alignedTimestamp - anchor.Timestamp).TotalSeconds;

			// Create deterministic noise that varies with time, seed, interval, drift, and volatility
			var timeHash = CreateDeterministicHash(alignedTimestamp, seed);
			var combinedSeed = seed ^ timeHash ^ _intervalHash ^ _paramHash;

			// Include interval ticks in the noise calculation to ensure different intervals produce different results
			var intervalAdjustedTime = t + (interval.TotalHours * 123.456); // Large multiplier to make interval differences more significant
			var intervalModifier = (int)(interval.TotalMinutes * 17); // Additional interval-based modifier
			var finalSeed = combinedSeed ^ intervalModifier;
			var noise = PseudoNoise(intervalAdjustedTime, finalSeed);			// GBM formula: price = anchor.Value * exp(drift * t + volatility * noise)
			// Convert time to hours for scaling
			var hoursFromAnchor = t / 3600.0;
			var logPrice = _hourlyDrift * hoursFromAnchor + _hourlyVolatility * noise;
			var price = (double)anchor.Value * Math.Exp(logPrice);

			// Generate OHLC values around the base price with interval-dependent variation
			var intervalMinutes = interval.TotalMinutes;
			var variationFactor = Math.Sqrt(intervalMinutes / 60.0) * 0.005; // Scale variation with interval

			// Special case: if we're exactly at the anchor timestamp, use anchor value as open price
			var openPrice = Math.Abs(t) < 1.0 ? (double)anchor.Value : price;
			var noise2 = PseudoNoise(intervalAdjustedTime + 100, finalSeed);
			var noise3 = PseudoNoise(intervalAdjustedTime + 200, finalSeed);
			var noise4 = PseudoNoise(intervalAdjustedTime + 300, finalSeed);

			var closePrice = openPrice * (1.0 + noise2 * variationFactor);
			var highPrice = Math.Max(openPrice, closePrice) * (1.0 + Math.Abs(noise3) * variationFactor);
			var lowPrice = Math.Min(openPrice, closePrice) * (1.0 - Math.Abs(noise4) * variationFactor);

			// Generate volume using deterministic hash-based approach
			var volumeHash = CreateDeterministicHash(alignedTimestamp.AddTicks(1), finalSeed);
			var volume = (volumeHash % 10000) + 1000; // Volume between 1000-11000

			return new Bar(
				Timestamp: timestamp, // Return the original requested timestamp, not the aligned one
				Open: (decimal)openPrice,
				High: (decimal)highPrice,
				Low: (decimal)lowPrice,
				Close: (decimal)closePrice,
				Volume: volume);
		}

		/// <summary>
		/// Creates deterministic pseudo-noise for the GBM stochastic term.
		/// Uses layered sine waves to simulate Brownian motion characteristics.
		/// </summary>
		/// <param name="t">The time parameter</param>
		/// <param name="seed">The seed for deterministic variation</param>
		/// <returns>A deterministic noise value between -1 and 1</returns>
		private static double PseudoNoise(double t, int seed)
		{
			// Use multiple frequencies to create more realistic noise
			var noise1 = Math.Sin(t * 0.1 + seed);
			var noise2 = Math.Sin(t * 0.05 + seed * 1.37) * 0.5;
			var noise3 = Math.Sin(t * 0.02 + seed * 2.17) * 0.25;

			// Normalize to approximately [-1, 1] range
			return (noise1 + noise2 + noise3) / 1.75;
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
			// For daily intervals (1440 minutes), align to the start of the day
			if (interval.TotalMinutes >= 1440)
			{
				return timestamp.Date;
			}
			
			// For multi-hour intervals (>= 60 minutes), align to hour boundaries
			if (interval.TotalMinutes >= 60)
			{
				// Use interval minutes to ensure precise alignment for intervals like 240 minutes (4 hours)
				var totalMinutes = timestamp.TimeOfDay.TotalMinutes;
				var intervalMinutes = interval.TotalMinutes;
				var alignedMinutes = Math.Floor(totalMinutes / intervalMinutes) * intervalMinutes;
				
				return timestamp.Date.AddMinutes(alignedMinutes);
			}
			
			// For minute intervals, use tick-based alignment
			var ticks = timestamp.Ticks;
			var intervalTicks = interval.Ticks;
			var alignedTicks = (ticks / intervalTicks) * intervalTicks;
			return new DateTime(alignedTicks, timestamp.Kind);
		}
	}
}
