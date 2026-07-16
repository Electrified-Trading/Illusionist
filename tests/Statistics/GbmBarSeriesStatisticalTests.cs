using Illusionist.Core.Catalog;

namespace Illusionist.Tests.Statistics;

/// <summary>
/// Runs the full statistical battery against <see cref="GbmBarSeries"/>. See
/// <see cref="StatisticalBarSeriesTestBase"/> for what each assertion catches and why.
/// </summary>
public sealed class GbmBarSeriesStatisticalTests : StatisticalBarSeriesTestBase
{
	protected override IBarSeriesFactory<OHLC> CreateFactory(int seed, double drift, double volatility)
		=> new GbmBarSeries.Factory("AAPL", seed, drift, volatility);
}
