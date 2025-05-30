namespace Illusionist.CLI.Infrastructure;

/// <summary>
/// Demo implementation of IBarSeriesFactory for scaffolding purposes.
/// TODO: Replace with actual implementation when generation math is implemented.
/// </summary>
public sealed class DemoBarSeriesFactory(int seed) : IBarSeriesFactory
{
	public IBarSeries GetSeries(TimeSpan interval)
	{
		return new DemoBarSeries(seed, interval);
	}
}

/// <summary>
/// Demo implementation of IBarSeries for scaffolding purposes.
/// TODO: Replace with actual implementation when generation math is implemented.
/// </summary>
internal sealed class DemoBarSeries(int seed, TimeSpan interval) : IBarSeries
{
	private readonly Random _random = new(seed);
	private decimal _currentPrice = 100.0m;

	public Bar GetBarAt(DateTime timestamp)
	{
		var bars = GetBars(timestamp).Take(1);
		return bars.First();
	}

	public IEnumerable<Bar> GetBars(DateTime start)
	{
		var current = start;

		while (true)
		{
			yield return GenerateBar(current);
			current = current.Add(interval);
		}
	}

	private Bar GenerateBar(DateTime timestamp)
	{
		// TODO: Replace with actual deterministic generation logic
		var open = _currentPrice;
		var change = (decimal)(_random.NextDouble() - 0.5) * 2.0m; // -1 to +1
		var high = open + Math.Abs(change) + (decimal)_random.NextDouble();
		var low = open - Math.Abs(change) - (decimal)_random.NextDouble();
		var close = open + change;
		var volume = (decimal)(_random.NextDouble() * 10000 + 1000);

		_currentPrice = close;

		return new Bar(timestamp, open, high, low, close, volume);
	}
}
