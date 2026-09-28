using Electrified.TimeSeries;

namespace Illusionist.Core.Catalog;

/// <summary>
/// <c>brownian-bridge@2</c>: the same Brownian-bridge geometric Brownian motion model as
/// <see cref="BrownianBridgeBarSeries"/> (<c>brownian-bridge@1</c>), with the same parameters and
/// the same algorithm, but every transcendental function (<c>log</c>, <c>exp</c>, <c>cos</c>)
/// replaced by <see cref="Illusionist.Core.Numerics.DeterministicMath"/>'s IEEE-754-only
/// implementation, so its output is byte-identical on every host -- not just x64 Windows.
/// </summary>
/// <remarks>
/// <para>
/// <c>brownian-bridge@1</c>'s golden fixtures did not reproduce on x64 Linux for two of ten cases
/// (see the change-log entry this version shipped with): <see cref="Math.Log(double)"/>,
/// <see cref="Math.Exp(double)"/> and <see cref="Math.Cos(double)"/> call the platform's own C
/// runtime (UCRT on Windows, glibc on Linux), which are not guaranteed to round identically in the
/// last bit for every input. A one-ulp difference in a single Gaussian draw propagates through the
/// bridge into a different bar.
/// </para>
/// <para>
/// <c>@1</c> stays exactly as published (D7: an existing version is never edited in place) -- this
/// is a new, independent version with its own registration and its own golden fixtures, deliberately
/// duplicating rather than sharing code with <see cref="BrownianBridgeBarSeries"/>, so <c>@1</c>'s
/// own frozen behavior can never be perturbed by a future change made in service of <c>@2</c>.
/// <see cref="Math.Sqrt(double)"/> is untouched here (and in <c>@1</c>): it is IEEE-754-mandated to
/// be correctly rounded on every conforming platform, so it carries none of <c>log</c>/<c>exp</c>/
/// <c>cos</c>'s cross-platform risk.
/// </para>
/// </remarks>
/// <param name="symbol">The trading symbol for this series</param>
/// <param name="seed">The random seed for deterministic generation</param>
/// <param name="schedule">The schedule defining valid bars</param>
/// <param name="anchor">The anchor point for time and price reference</param>
/// <param name="drift">The drift parameter for GBM (default: 0.0001)</param>
/// <param name="volatility">The volatility parameter for GBM (default: 0.01)</param>
public sealed partial class BrownianBridgeBarSeriesV2(
	string symbol,
	int seed,
	ISchedule schedule,
	BarAnchor anchor,
	double drift = 0.0001,
	double volatility = 0.01) : IBarSeries<OHLC>
{
	private readonly Generator _generator = new(seed + GetDeterministicHashCode(symbol), schedule, drift, volatility, anchor);

	/// <summary>
	/// Computes a deterministic hash code for a string that is stable across .NET processes.
	/// Unlike <see cref="string.GetHashCode()"/>, which is randomized per-process in .NET 5+,
	/// this uses DJB2 to produce a consistent hash.
	/// </summary>
	/// <remarks>
	/// DJB2 (Bernstein hash) computed over the string's UTF-16 <c>char</c> values, not
	/// <see cref="string.GetHashCode()"/> or any platform-specific byte encoding -- the same DJB2
	/// variant, applied to the same <c>char</c> sequence, produces the same integer on any .NET
	/// process, on any machine, forever.
	/// </remarks>
	private static int GetDeterministicHashCode(string value)
	{
		unchecked
		{
			int hash = 5381;
			foreach (char c in value)
				hash = ((hash << 5) + hash) + c;

			return hash;
		}
	}

	/// <summary>
	/// Gets the bar that contains or immediately precedes the specified timestamp.
	/// Uses Geometric Brownian Motion to generate realistic price evolution.
	/// </summary>
	/// <param name="timestamp">The timestamp to query</param>
	/// <returns>The bar for the specified timestamp</returns>
	public Bar<OHLC> GetBarAt(DateTime timestamp)
	{
		return _generator.GetBarAt(timestamp);
	}

	/// <summary>
	/// Gets an enumerable sequence of bars starting from the specified timestamp.
	/// Each bar follows GBM price evolution and uses schedule-aware time advancement.
	/// </summary>
	/// <param name="start">The starting timestamp</param>
	/// <returns>An enumerable sequence of bars</returns>
	public IEnumerable<Bar<OHLC>> GetBars(DateTime start)
	{
		var current = start;
		var firstBar = _generator.GetBarAt(current);
		yield return firstBar;

		// Use schedule-aware time advancement
		while (true)
		{
			current = _generator.Schedule.GetNextValidBarTime(current);
			yield return _generator.GetBarAt(current);
		}
	}
}
