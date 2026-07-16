using Electrified.TimeSeries;

namespace Illusionist.Core.Catalog;

public sealed partial class GbmBarSeries
{
	/// <summary>
	/// Generates deterministic bars using a hash-seeded Brownian bridge.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <see cref="GetBarAt"/> must be stateless random access: any timestamp, O(log elapsed
	/// time), no memory of prior calls. A true random walk is inherently sequential -- bar
	/// 10,000 is the sum of 10,000 prior increments -- so satisfying both constraints at once
	/// requires more than a closed-form function of elapsed time. The prior implementation
	/// solved that the wrong way: a sum of three fixed-frequency sine waves, bounded and
	/// non-cumulative -- a stationary series, not a random walk, regardless of how plausible the
	/// resulting candles looked.
	/// </para>
	/// <para>
	/// The reconciliation is a Brownian bridge: fix Gaussian increments at power-of-two
	/// "checkpoint" times via a deterministic hash, then bisect between the nearest pair of
	/// checkpoints down to the exact query point via midpoint displacement -- the standard,
	/// exact method for simulating one sample path of a Wiener process. Both steps are
	/// O(log elapsed seconds); the hash function is the only "memory" the process needs.
	/// </para>
	/// <para>
	/// <b>High and Low are a deliberate simplification, not sampled from the exact bridge-extremum
	/// distribution.</b> They are built from a volatility-scaled perturbation around the bar's
	/// open/midpoint/close, which guarantees the standard OHLC invariants and responds correctly
	/// to the volatility parameter, but is not statistically exact for tail extremal behaviour.
	/// </para>
	/// </remarks>
	/// <param name="seed">The random seed for deterministic generation</param>
	/// <param name="schedule">The schedule defining valid bars</param>
	/// <param name="drift">The drift parameter (annualized)</param>
	/// <param name="volatility">The volatility parameter (annualized)</param>
	/// <param name="anchor">The anchor point for time and price reference</param>
	public sealed class Generator(int seed, ISchedule schedule, double drift, double volatility, BarAnchor anchor)
	{
		private const double SecondsPerYear = 365.25 * 24 * 3600;

		private readonly double _driftPerSecond = drift / SecondsPerYear;
		private readonly double _volatilityPerSecond = volatility / Math.Sqrt(SecondsPerYear);

		/// <summary>
		/// Gets the time interval between bars.
		/// </summary>
		public TimeSpan Interval { get; } = GetIntervalFromSchedule(schedule);

		/// <summary>
		/// Gets the schedule for valid bars.
		/// </summary>
		public ISchedule Schedule { get; } = schedule;

		/// <summary>
		/// Extracts the time interval from a schedule that supports DefaultEquitiesSchedule.
		/// </summary>
		private static TimeSpan GetIntervalFromSchedule(ISchedule schedule)
		{
			return schedule switch
			{
				DefaultEquitiesSchedule equitiesSchedule => equitiesSchedule.Interval.Interval,
				_ => throw new ArgumentException($"Unsupported schedule type: {schedule.GetType()}")
			};
		}

		/// <summary>
		/// Generates a deterministic bar at the specified timestamp, aligned to the bar
		/// interval boundary. The close chains exactly to the next bar's open.
		/// </summary>
		/// <param name="timestamp">The timestamp to generate a bar for</param>
		/// <returns>A deterministic bar with OHLCV data</returns>
		public Bar<OHLC> GetBarAt(DateTime timestamp)
		{
			var alignedTimestamp = AlignToInterval(timestamp);
			var nextTimestamp = Schedule.GetNextValidBarTime(alignedTimestamp);

			var open = PriceAt(alignedTimestamp);
			var close = PriceAt(nextTimestamp);

			var midTicks = alignedTimestamp.Ticks + (nextTimestamp.Ticks - alignedTimestamp.Ticks) / 2;
			var mid = PriceAt(new DateTime(midTicks, alignedTimestamp.Kind));

			var barSeconds = Math.Max(1.0, (nextTimestamp - alignedTimestamp).TotalSeconds);
			var localScale = (double)open * _volatilityPerSecond * Math.Sqrt(barSeconds);
			var t = ElapsedSeconds(alignedTimestamp);

			var highBase = Max3(open, mid, close);
			var lowBase = Min3(open, mid, close);
			var high = highBase + (decimal)(localScale * Math.Abs(GaussianAt(t, salt: 101)));
			var low = lowBase - (decimal)(localScale * Math.Abs(GaussianAt(t, salt: 202)));
			if (low <= 0)
				low = lowBase * 0.5m; // Defensive: only reachable at pathological (near-zero price, extreme volatility) combinations.

			var volumeHash = MixHash(seed, t, salt: 303);
			var volume = (long)(volumeHash % 10000) + 1000;

			return Bar.Create(
				timestamp, // Return the original requested timestamp, not the aligned one.
				new OHLC { Open = open, High = high, Low = low, Close = close },
				volume);
		}

		private decimal PriceAt(DateTime timestamp)
		{
			var t = ElapsedSeconds(timestamp);
			var logDrift = _driftPerSecond * t;
			var logOffset = _volatilityPerSecond * BrownianOffset(t);
			return anchor.Value * (decimal)Math.Exp(logDrift + logOffset);
		}

		private long ElapsedSeconds(DateTime timestamp)
			=> (long)Math.Round((timestamp - anchor.Timestamp).TotalSeconds, MidpointRounding.AwayFromZero);

		/// <summary>
		/// Value of a unit-variance-per-second Wiener process at <paramref name="t"/> seconds
		/// from the anchor (0 by definition). Builds power-of-two checkpoints outward from the
		/// anchor, then bisects down to the exact target -- O(log |t|), deterministic in
		/// (seed, t) only.
		/// </summary>
		private double BrownianOffset(long t)
		{
			if (t == 0)
				return 0.0;

			var sign = Math.Sign(t);
			var magnitude = Math.Abs(t);

			var coord = (long)sign;
			var value = GaussianAt(coord, salt: 0);

			if (magnitude == 1)
				return value;

			var priorCoord = 0L;
			var priorValue = 0.0;

			while (Math.Abs(coord) < magnitude)
			{
				priorCoord = coord;
				priorValue = value;
				coord *= 2;
				value = priorValue + GaussianAt(coord, salt: 0) * Math.Sqrt(Math.Abs(coord - priorCoord));
			}

			return coord == t
				? value
				: Bridge(priorCoord, coord, t, priorValue, value);
		}

		/// <summary>
		/// Standard Brownian-bridge midpoint displacement between two known checkpoints,
		/// descending to the exact target coordinate.
		/// </summary>
		private double Bridge(long lo, long hi, long target, double valueAtLo, double valueAtHi)
		{
			while (true)
			{
				if (target == lo)
					return valueAtLo;

				if (target == hi)
					return valueAtHi;

				var mid = lo + (hi - lo) / 2;
				var z = GaussianAt(mid, salt: 1);
				var valueAtMid = (valueAtLo + valueAtHi) / 2.0 + z * Math.Sqrt(Math.Abs(hi - lo) / 4.0);

				if (Math.Abs(target - lo) < Math.Abs(mid - lo))
				{
					hi = mid;
					valueAtHi = valueAtMid;
				}
				else
				{
					lo = mid;
					valueAtLo = valueAtMid;
				}
			}
		}

		/// <summary>
		/// Deterministic standard normal draw for a given coordinate and purpose (Box-Muller
		/// over two independently salted hashes of the same key -- exact, not an approximation,
		/// given genuinely independent uniforms).
		/// </summary>
		private double GaussianAt(long coord, int salt)
		{
			var h1 = MixHash(seed, coord, salt * 2);
			var h2 = MixHash(seed, coord, salt * 2 + 1);

			var u1 = (h1 + 0.5) / 4294967296.0; // in (0,1); the +0.5 keeps log(u1) finite.
			var u2 = (h2 + 0.5) / 4294967296.0;

			return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
		}

		/// <summary>
		/// Combines a seed, coordinate, and purpose salt into one hash, finished with the
		/// MurmurHash3 32-bit avalanche mixer.
		/// </summary>
		private static uint MixHash(int seed, long coord, int salt)
		{
			unchecked
			{
				var h = (uint)seed;
				h = Combine(h, (uint)(coord ^ (coord >> 32)));
				h = Combine(h, (uint)salt);

				h ^= h >> 16;
				h *= 0x85ebca6b;
				h ^= h >> 13;
				h *= 0xc2b2ae35;
				h ^= h >> 16;
				return h;
			}
		}

		private static uint Combine(uint seed, uint value)
		{
			unchecked
			{
				return seed ^ (value + 0x9e3779b9 + (seed << 6) + (seed >> 2));
			}
		}

		private static decimal Max3(decimal a, decimal b, decimal c) => Math.Max(a, Math.Max(b, c));

		private static decimal Min3(decimal a, decimal b, decimal c) => Math.Min(a, Math.Min(b, c));

		/// <summary>
		/// Aligns the given timestamp to the previous interval boundary.
		/// For sub-day intervals, aligns within the day. For daily+ intervals, aligns to day start.
		/// </summary>
		/// <param name="timestamp">The timestamp to align</param>
		/// <returns>The aligned timestamp at the previous interval boundary</returns>
		private DateTime AlignToInterval(DateTime timestamp)
		{
			if (Interval >= TimeSpan.FromDays(1))
			{
				// Align to market open, not midnight: DefaultEquitiesSchedule.GetNextValidBarTime
				// produces 9:30 AM for daily bars, and Close-to-Open chaining requires this
				// method to agree with the schedule's own notion of a bar's start.
				return timestamp.Date.Add(DefaultEquitiesSchedule.MarketOpen.ToTimeSpan());
			}

			// For sub-day intervals, align within the day
			var timeOfDay = timestamp.TimeOfDay;
			var intervalTicks = Interval.Ticks;
			// Quantized to the nearest interval boundary
			var intervals = timeOfDay.Ticks / intervalTicks;

			// Total quantized ticks for the aligned time
			var alignedTicks = intervals * intervalTicks;
			return timestamp.Date.Add(new TimeSpan(alignedTicks));
		}
	}
}
