namespace Illusionist.Tests.Statistics;

public abstract partial class StatisticalBarSeriesTestBase
{
	/// <summary>
	/// With volatility fixed at zero, over a window where every bar-to-bar time delta is
	/// identical (one trading session, no weekend/holiday jump), the stochastic term must vanish
	/// entirely: log returns become a constant (the drift increment), and their sample variance
	/// is exactly zero. The prior generator's movement came from a hardcoded 0.005 multiplier
	/// applied regardless of this parameter -- setting volatility to 0 changed nothing.
	/// </summary>
	[Fact]
	public void Volatility_Zero_ProducesZeroVarianceReturns()
	{
		var bars = GenerateSameSessionMinuteBars(CreateFactory(Seed, drift: 0.0001, volatility: 0.0), 100);
		var r = LogReturns(bars);

		var variance = Variance(r);

		Assert.True(variance < 1e-18, $"Var(returns | volatility=0) = {variance:E4}, expected ~0.");
	}

	/// <summary>
	/// Increasing volatility must increase realized return variance. Not pinned to the exact
	/// theoretical ratio (vol^2 scaling) to avoid a brittle assertion on one finite sample --
	/// only that the parameter visibly does something, which the prior generator's did not
	/// (extreme volatility=5, 500x the default, barely moved the output).
	/// </summary>
	[Fact]
	public void Volatility_Increasing_IncreasesReturnVariance()
	{
		var low = GenerateSameSessionMinuteBars(CreateFactory(Seed, drift: 0.0001, volatility: 0.01), 100);
		var high = GenerateSameSessionMinuteBars(CreateFactory(Seed, drift: 0.0001, volatility: 0.05), 100);

		var varLow = Variance(LogReturns(low));
		var varHigh = Variance(LogReturns(high));

		Assert.True(
			varHigh > varLow * 5,
			$"Var(returns | vol=0.05) = {varHigh:E4} should exceed 5x Var(returns | vol=0.01) = {varLow:E4}.");
	}
}
