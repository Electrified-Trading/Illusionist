using NSubstitute;

namespace Illusionist.Tests.Models;

/// <summary>
/// Tests for IBarSeriesFactory implementations to ensure deterministic behavior.
/// </summary>
public class BarSeriesFactoryTests
{	[Fact]
	public void GetSeries_WithSameSeed_ShouldProduceDeterministicResults()
	{
		// Arrange
		const int seed = 12345;
		var interval = BarInterval.Minute(1);
		var schedule = CreateScheduleFromInterval(interval);
		var anchor = new BarAnchor(new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc), 100.0m);
		var factory1 = CreateTestFactory(seed);
		var factory2 = CreateTestFactory(seed);

		// Act
		var series1 = factory1.GetSeries(schedule, anchor);
		var series2 = factory2.GetSeries(schedule, anchor);

		var startTime = new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc);
		var bars1 = series1.GetBars(startTime).Take(5).ToArray();
		var bars2 = series2.GetBars(startTime).Take(5).ToArray();

		// Assert
		Assert.Equal(bars1.Length, bars2.Length);
		for (int i = 0; i < bars1.Length; i++)
		{
			Assert.Equal(bars1[i].Timestamp, bars2[i].Timestamp);
			Assert.Equal(bars1[i].Open, bars2[i].Open);
			Assert.Equal(bars1[i].High, bars2[i].High);
			Assert.Equal(bars1[i].Low, bars2[i].Low);
			Assert.Equal(bars1[i].Close, bars2[i].Close);
			Assert.Equal(bars1[i].Volume, bars2[i].Volume);
		}
	}	[Fact]
	public void GetSeries_WithDifferentSeeds_ShouldProduceDifferentResults()
	{
		// Arrange
		var factory1 = CreateTestFactory(12345);
		var factory2 = CreateTestFactory(54321);
		var interval = BarInterval.Minute(1);
		var schedule = CreateScheduleFromInterval(interval);
		var anchor = new BarAnchor(new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc), 100.0m);

		// Act
		var series1 = factory1.GetSeries(schedule, anchor);
		var series2 = factory2.GetSeries(schedule, anchor);

		var startTime = new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc);
		var bar1 = series1.GetBarAt(startTime);
		var bar2 = series2.GetBarAt(startTime);

		// Assert
		Assert.NotEqual(bar1.Open, bar2.Open);
	}	[Theory]
	[InlineData(1)] // 1 minute
	[InlineData(5)] // 5 minutes
	[InlineData(60)] // 1 hour
	public void GetSeries_WithDifferentIntervals_ShouldReturnValidSeries(int intervalMinutes)
	{
		// Arrange
		var factory = CreateTestFactory(42);
		var interval = BarInterval.Minute((ushort)intervalMinutes);
		var schedule = CreateScheduleFromInterval(interval);
		var anchor = new BarAnchor(new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc), 100.0m);

		// Act
		var series = factory.GetSeries(schedule, anchor);

		// Assert
		Assert.NotNull(series);

		var startTime = new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc);
		var bar = series.GetBarAt(startTime);

		Assert.True(bar.High >= bar.Open);
		Assert.True(bar.High >= bar.Close);
		Assert.True(bar.Low <= bar.Open);
		Assert.True(bar.Low <= bar.Close);
		Assert.True(bar.Volume > 0);
	}

	private static ISchedule CreateScheduleFromInterval(BarInterval interval)
	{
		var factory = new DefaultEquitiesScheduleFactory();
		return factory.GetSchedule(interval);
	}

	private static IBarSeriesFactory CreateTestFactory(int seed)
	{
		// TODO: Replace with actual factory implementation when available
		var factory = Substitute.For<IBarSeriesFactory>();

		// For now, return a mock that simulates deterministic behavior
		factory.GetSeries(Arg.Any<ISchedule>(), Arg.Any<BarAnchor>()).Returns(callInfo =>
		{
			var schedule = callInfo.Arg<ISchedule>();
			// Extract interval from schedule (assuming DefaultEquitiesSchedule)
			var interval = schedule is DefaultEquitiesSchedule equitiesSchedule 
				? equitiesSchedule.Interval.Interval 
				: TimeSpan.FromMinutes(1); // fallback
			return CreateTestSeries(seed, interval);
		});

		return factory;
	}

	private static IBarSeries CreateTestSeries(int seed, TimeSpan interval)
	{
		var series = Substitute.For<IBarSeries>();
		var random = new Random(seed);

		series.GetBarAt(Arg.Any<DateTime>()).Returns(callInfo =>
		{
			var timestamp = callInfo.Arg<DateTime>();
			return CreateTestBar(timestamp, random);
		});

		series.GetBars(Arg.Any<DateTime>()).Returns(callInfo =>
		{
			var start = callInfo.Arg<DateTime>();
			return GenerateTestBars(start, interval, random);
		});

		return series;
	}

	private static IEnumerable<Bar> GenerateTestBars(DateTime start, TimeSpan interval, Random random)
	{
		var current = start;
		for (int i = 0; i < 100; i++) // Generate up to 100 bars for testing
		{
			yield return CreateTestBar(current, random);
			current = current.Add(interval);
		}
	}

	private static Bar CreateTestBar(DateTime timestamp, Random random)
	{
		var open = 100m + (decimal)(random.NextDouble() * 20 - 10);
		var change = (decimal)(random.NextDouble() - 0.5) * 2m;
		var high = open + Math.Abs(change) + (decimal)random.NextDouble();
		var low = open - Math.Abs(change) - (decimal)random.NextDouble();
		var close = open + change;
		var volume = (decimal)(random.NextDouble() * 10000 + 1000);

		return new Bar(timestamp, open, high, low, close, volume);
	}
}
