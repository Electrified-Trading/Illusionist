namespace Illusionist.Tests.Statistics;

public abstract partial class StatisticalBarSeriesTestBase
{
	[Fact]
	public void VarianceRatio_AtHorizonOne_IsExactlyOneByConstruction()
	{
		var bars = GenerateDailyBars(CreateFactory(Seed, drift: 0.0001, volatility: 0.01), SampleSize);
		var r = LogReturns(bars);

		var vr1 = VarianceRatio(r, 1);

		// Sanity check on the estimator itself, not the generator: VR(1) collapses to the
		// 1-period variance divided by itself. If this fails, the estimator is broken.
		Assert.Equal(1.0, vr1, precision: 9);
	}

	/// <summary>
	/// A random walk holds VR(k) &#8776; 1 at every horizon. The prior generator decayed as
	/// ~1/k (0.49, 0.24, 0.12, 0.06, 0.02 at k=2,4,8,16,32) -- a stationary series, not one that
	/// can wander. Tolerance is the Lo-MacKinlay 99% asymptotic band, fixed before any corrected
	/// generator existed.
	/// </summary>
	[Theory]
	[InlineData(2)]
	[InlineData(4)]
	[InlineData(8)]
	[InlineData(16)]
	[InlineData(32)]
	public void VarianceRatio_ApproximatelyOne_AcrossHorizons(int k)
	{
		var bars = GenerateDailyBars(CreateFactory(Seed, drift: 0.0001, volatility: 0.01), SampleSize);
		var r = LogReturns(bars);
		var nq = r.Length;

		var vr = VarianceRatio(r, k);
		var se = LoMacKinlayStandardError(k, nq);
		var band = ZScore99 * se;

		Assert.True(
			vr is > 0 && Math.Abs(vr - 1.0) <= band,
			$"VR({k}) = {vr:F4}, expected within [{1 - band:F4}, {1 + band:F4}] (99% band, SE={se:F4}, nq={nq}).");
	}
}
