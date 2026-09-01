using Illusionist.Core.Catalog;

namespace Illusionist.Tests.Statistics;

/// <summary>
/// Runs the full statistical battery against <see cref="BrownianBridgeBarSeries"/>. See
/// <see cref="StatisticalBarSeriesTestBase"/> for what each assertion catches and why.
/// </summary>
public sealed class BrownianBridgeBarSeriesStatisticalTests : StatisticalBarSeriesTestBase
{
	protected override IBarSeriesFactory<OHLC> CreateFactory(int seed, double drift, double volatility)
		=> new BrownianBridgeBarSeries.Factory("AAPL", seed, drift, volatility);
}
