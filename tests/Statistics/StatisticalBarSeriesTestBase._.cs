namespace Illusionist.Tests.Statistics;

/// <summary>
/// Base class for testing that an <see cref="IBarSeriesFactory{T}"/> produces bars with
/// genuine random-walk statistics, not merely well-formed OHLC values.
/// </summary>
/// <remarks>
/// <para>
/// AlphaHawk's original GBM generator passed 53 structural tests (High &gt;= Open, Volume &gt; 0,
/// determinism) for a year while emitting a series with none of the statistical properties of a
/// random walk: setting <c>volatility: 0</c> changed nothing, the variance ratio decayed as ~1/k,
/// and lag-1 autocorrelation sat at -0.50 -- the signature of differenced i.i.d. noise, not a
/// price path. Every assertion here targets exactly one of those failures, and every tolerance is
/// derived from the statistic's known sampling distribution under a true random walk -- fixed
/// before any corrected generator exists, so it cannot be fitted to one.
/// </para>
/// <para>
/// Inheritors implement <see cref="CreateFactory"/> for the generator under test.
/// </para>
/// </remarks>
public abstract partial class StatisticalBarSeriesTestBase
{
	/// <summary>Bars generated per sample. Fixes n = 999 one-step log returns.</summary>
	protected const int SampleSize = 1000;

	/// <summary>Fixed seed, committed before any generator output was inspected.</summary>
	protected const int Seed = 42;

	/// <summary>
	/// Two-tailed 99% critical value. Chosen wide on purpose: with one committed seed and no
	/// re-rolling, the band must tolerate a single random walk's realized sampling noise without
	/// being loose enough to pass a broken generator.
	/// </summary>
	private const double ZScore99 = 2.5758293035489004;

	/// <summary>
	/// Creates the factory under test with explicit drift and volatility, so every subclass
	/// generator is exercised identically regardless of its own default parameters.
	/// </summary>
	protected abstract IBarSeriesFactory<OHLC> CreateFactory(int seed, double drift, double volatility);

	private static List<Bar<OHLC>> GenerateDailyBars(IBarSeriesFactory<OHLC> factory, int count)
	{
		var anchor = new BarAnchor(new DateTime(2024, 1, 2, 9, 30, 0), 100m);
		var schedule = new DefaultEquitiesScheduleFactory().GetSchedule(BarInterval.Day(1));
		var series = factory.GetSeries(schedule, anchor);
		return series.GetBars(anchor.Timestamp).Take(count).ToList();
	}

	private static List<Bar<OHLC>> GenerateSameSessionMinuteBars(IBarSeriesFactory<OHLC> factory, int count)
	{
		// Entirely inside one trading session (9:30-16:00): every consecutive pair is
		// exactly one minute apart, so drift contributes an identical increment to every
		// return. Any variance beyond that is attributable to volatility alone.
		var anchor = new BarAnchor(new DateTime(2024, 1, 2, 9, 30, 0), 100m);
		var schedule = new DefaultEquitiesScheduleFactory().GetSchedule(BarInterval.Minute(1));
		var series = factory.GetSeries(schedule, anchor);
		return series.GetBars(anchor.Timestamp).Take(count).ToList();
	}

	private static double[] LogReturns(IReadOnlyList<Bar<OHLC>> bars)
	{
		var r = new double[bars.Count - 1];
		for (var t = 1; t < bars.Count; t++)
			r[t - 1] = Math.Log((double)bars[t].Data.Close) - Math.Log((double)bars[t - 1].Data.Close);

		return r;
	}

	/// <summary>
	/// Lo &amp; MacKinlay (1988) overlapping variance-ratio estimator.
	/// VR(1) = 1 exactly by construction -- verify that identity before trusting VR(k&gt;1).
	/// </summary>
	private static double VarianceRatio(double[] r, int k)
	{
		var nq = r.Length;
		var mu = r.Average();

		var sumSqA = 0d;
		foreach (var x in r)
		{
			var d = x - mu;
			sumSqA += d * d;
		}

		var sigmaA2 = sumSqA / (nq - 1);

		var x0 = new double[nq + 1];
		for (var t = 1; t <= nq; t++)
			x0[t] = x0[t - 1] + r[t - 1];

		var sumSqB = 0d;
		for (var t = k; t <= nq; t++)
		{
			var d = x0[t] - x0[t - k] - k * mu;
			sumSqB += d * d;
		}

		var m = k * (nq - k + 1) * (1.0 - (double)k / nq);
		var sigmaB2 = sumSqB / m;

		return sigmaB2 / sigmaA2;
	}

	/// <summary>
	/// Asymptotic standard error of VR(k) under the null of a homoskedastic random walk
	/// (Lo &amp; MacKinlay 1988, eq. 2.9): sqrt(2(2k-1)(k-1) / (3k*nq)).
	/// </summary>
	private static double LoMacKinlayStandardError(int k, int nq)
		=> Math.Sqrt(2.0 * (2 * k - 1) * (k - 1) / (3.0 * k * nq));

	private static double Autocorrelation(double[] r, int lag)
	{
		var n = r.Length;
		var mu = r.Average();
		double num = 0, den = 0;
		for (var i = 0; i < n; i++)
		{
			var d = r[i] - mu;
			den += d * d;
		}

		for (var i = 0; i < n - lag; i++)
			num += (r[i] - mu) * (r[i + lag] - mu);

		return num / den;
	}

	/// <summary>Bartlett's formula for the standard error of a sample autocorrelation under white noise.</summary>
	private static double BartlettStandardError(int n)
		=> 1.0 / Math.Sqrt(n);

	private static double Variance(double[] x)
	{
		var mu = x.Average();
		double sumSq = 0;
		foreach (var v in x)
		{
			var d = v - mu;
			sumSq += d * d;
		}

		return sumSq / (x.Length - 1);
	}
}
