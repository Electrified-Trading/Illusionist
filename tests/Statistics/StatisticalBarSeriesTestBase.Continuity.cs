namespace Illusionist.Tests.Statistics;

public abstract partial class StatisticalBarSeriesTestBase
{
	/// <summary>
	/// Consecutive bars must chain: the next bar opens where the prior one closed. The prior
	/// generator re-anchored every open to the deterministic trend while the close scattered
	/// independently -- mean gap 0.95%, 0 of 999 bars continuous. A picket fence, not a chart.
	/// </summary>
	/// <remarks>
	/// This assertion is guaranteed by construction once the generator under test chains its
	/// bars explicitly -- it is not a statistical test with a sampling distribution, unlike its
	/// siblings in this suite. Its value is as a regression guard, not a discovery.
	/// </remarks>
	[Fact]
	public void Continuity_ConsecutiveBars_OpenEqualsPriorClose()
	{
		var bars = GenerateDailyBars(CreateFactory(Seed, drift: 0.0001, volatility: 0.01), SampleSize);

		var discontinuous = 0;
		for (var i = 1; i < bars.Count; i++)
		{
			if (bars[i].Data.Open != bars[i - 1].Data.Close)
				discontinuous++;
		}

		Assert.True(discontinuous == 0, $"{discontinuous} of {bars.Count - 1} bars are discontinuous.");
	}
}
