namespace Illusionist.Core.Catalog;

public sealed partial class GbmBarSeries
{
    /// <summary>
    /// Generator that implements Geometric Brownian Motion for deterministic price evolution.
    /// Uses log-normal distribution characteristics to create realistic market-like behavior.
    /// </summary>
    public sealed class Generator
    {
        private readonly string _symbol;
        private readonly int _seed;
        private readonly TimeSpan _interval;
        private readonly double _drift;
        private readonly double _volatility;
        private static readonly DateTime Epoch = DateTime.UnixEpoch;

        /// <summary>
        /// Gets the time interval between bars.
        /// </summary>
        public TimeSpan Interval => _interval;

        /// <summary>
        /// Initializes a new instance of the <see cref="Generator"/> class.
        /// </summary>
        /// <param name="symbol">The trading symbol</param>
        /// <param name="seed">The random seed for deterministic generation</param>
        /// <param name="interval">The time interval between bars</param>
        /// <param name="drift">The drift parameter for GBM</param>
        /// <param name="volatility">The volatility parameter for GBM</param>
        public Generator(string symbol, int seed, TimeSpan interval, double drift, double volatility)
        {
            _symbol = symbol;
            _seed = seed;
            _interval = interval;
            _drift = drift;
            _volatility = volatility;        }

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
            
            // Convert to time units for mathematical operations (using hours for better scaling)
            var hoursFromEpoch = (alignedTimestamp - Epoch).TotalHours;
            
            // Scale parameters to work with hourly time units and make them more reasonable
            var hourlyDrift = _drift / (365.25 * 24); // Convert annual drift to hourly
            var hourlyVolatility = _volatility / Math.Sqrt(365.25 * 24); // Convert annual volatility to hourly
            
            // Create deterministic noise that varies with time, seed, interval, drift, and volatility
            var timeHash = CreateDeterministicHash(alignedTimestamp, _seed);
            var intervalHash = _interval.Ticks.GetHashCode();
            var paramHash = (_drift.GetHashCode() ^ _volatility.GetHashCode());
            var combinedSeed = _seed ^ timeHash ^ intervalHash ^ paramHash;
            
            var noise = PseudoNoise(hoursFromEpoch, combinedSeed);
              // GBM formula: S(t) = S(0) * exp((μ - σ²/2) * t + σ * W(t))
            // Start with a base price of 1.0 for mathematical modeling
            var basePriceLog = Math.Log(1.0); // 0.0
            var logPrice = basePriceLog + (hourlyDrift - (hourlyVolatility * hourlyVolatility / 2.0)) * hoursFromEpoch + hourlyVolatility * noise;
            
            // Clamp logPrice to keep prices in reasonable range (0.1 to 10.0)
            logPrice = Math.Max(Math.Log(0.1), Math.Min(Math.Log(10.0), logPrice));
            
            var basePrice = Math.Exp(logPrice);
            
            // Generate OHLC values around the base price with interval-dependent variation
            var intervalMinutes = _interval.TotalMinutes;
            var variationFactor = Math.Sqrt(intervalMinutes / 60.0) * 0.005; // Scale variation with interval
            
            var openPrice = basePrice;
            var noise2 = PseudoNoise(hoursFromEpoch + 100, combinedSeed);
            var noise3 = PseudoNoise(hoursFromEpoch + 200, combinedSeed);
            var noise4 = PseudoNoise(hoursFromEpoch + 300, combinedSeed);
            
            var closePrice = openPrice * (1.0 + noise2 * variationFactor);
            var highPrice = Math.Max(openPrice, closePrice) * (1.0 + Math.Abs(noise3) * variationFactor);
            var lowPrice = Math.Min(openPrice, closePrice) * (1.0 - Math.Abs(noise4) * variationFactor);
            
            // Generate volume using deterministic hash-based approach
            var volumeHash = CreateDeterministicHash(alignedTimestamp.AddTicks(1), combinedSeed);
            var volume = (volumeHash % 10000) + 1000; // Volume between 1000-11000
            
            return new Bar(
                Timestamp: alignedTimestamp,
                Open: (decimal)openPrice,
                High: (decimal)highPrice,
                Low: (decimal)lowPrice,
                Close: (decimal)closePrice,
                Volume: (decimal)volume);        }

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
            var ticks = timestamp.Ticks;
            var intervalTicks = _interval.Ticks;
            var alignedTicks = (ticks / intervalTicks) * intervalTicks;
            return new DateTime(alignedTicks, timestamp.Kind);
        }
    }
}
