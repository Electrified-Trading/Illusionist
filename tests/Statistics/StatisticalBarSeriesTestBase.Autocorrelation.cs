namespace Illusionist.Tests.Statistics;

public abstract partial class StatisticalBarSeriesTestBase
{
	/// <summary>
	/// A random walk's returns are uncorrelated: acf(1) &#8776; 0. The prior generator measured
	/// exactly -0.50 -- the textbook signature of differencing i.i.d. noise, meaning the "close"
	/// was independent noise around a trend, not a price path. Tolerance is Bartlett's formula at
	/// the 99% band.
	/// </summary>
	[Fact]
	public void Autocorrelation_AtLagOne_ApproximatelyZero()
	{
		var bars = GenerateDailyBars(CreateFactory(Seed, drift: 0.0001, volatility: 0.01), SampleSize);
		var r = LogReturns(bars);

		var acf1 = Autocorrelation(r, 1);
		var band = ZScore99 * BartlettStandardError(r.Length);

		Assert.True(
			Math.Abs(acf1) <= band,
			$"acf(1) = {acf1:F4}, expected within [{-band:F4}, {band:F4}] (99% Bartlett band, n={r.Length}).");
	}
}
