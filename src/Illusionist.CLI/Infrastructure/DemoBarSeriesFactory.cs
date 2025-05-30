using Illusionist.Core.Catalog;

namespace Illusionist.CLI.Infrastructure;

/// <summary>
/// Factory implementation using SeededBarSeriesFactory for deterministic bar generation.
/// Provides reproducible OHLCV data for demonstration and testing purposes.
/// </summary>
public sealed class DemoBarSeriesFactory(int seed) : IBarSeriesFactory
{
	private readonly SeededBarSeries.Factory _factory = new(seed);

	public IBarSeries GetSeries(TimeSpan interval)
	{
		return _factory.GetSeries(interval);
	}
}
