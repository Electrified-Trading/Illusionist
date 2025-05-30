using Illusionist.Core.Catalog;

namespace Illusionist.Tests.Models;

/// <summary>
/// Tests for IBarSeries implementations to ensure deterministic behavior
/// and correct bar series generation.
/// </summary>
public class IBarSeriesTests
{
	[Fact]
	public void GetBarAt_SameSeedAndTimestamp_ReturnsIdenticalBars()
	{
		// Arrange
		const int seed = 12345;
		var interval = TimeSpan.FromMinutes(1);
		var timestamp = new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc);

		var factory1 = new SeededBarSeries.Factory(seed);
		var factory2 = new SeededBarSeries.Factory(seed);
		var series1 = factory1.GetSeries(interval);
		var series2 = factory2.GetSeries(interval);

		// Act
		var bar1 = series1.GetBarAt(timestamp);
		var bar2 = series2.GetBarAt(timestamp);

		// Assert
		Assert.Equal(bar1, bar2);
	}

	[Fact]
	public void GetBarAt_DifferentSeeds_ReturnsDifferentBars()
	{
		// Arrange
		const int seed1 = 12345;
		const int seed2 = 54321;
		var interval = TimeSpan.FromMinutes(1);
		var timestamp = new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc);

		var factory1 = new SeededBarSeries.Factory(seed1);
		var factory2 = new SeededBarSeries.Factory(seed2);
		var series1 = factory1.GetSeries(interval);
		var series2 = factory2.GetSeries(interval);

		// Act
		var bar1 = series1.GetBarAt(timestamp);
		var bar2 = series2.GetBarAt(timestamp);

		// Assert
		Assert.NotEqual(bar1, bar2);
	}

	[Fact]
	public void GetBars_MultipleCalls_ReturnsIdenticalSequences()
	{
		// Arrange
		const int seed = 12345;
		var interval = TimeSpan.FromMinutes(5);
		var startTime = new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc);

		var factory = new SeededBarSeries.Factory(seed);
		var series1 = factory.GetSeries(interval);
		var series2 = factory.GetSeries(interval);

		// Act
		var bars1 = series1.GetBars(startTime).Take(10).ToList();
		var bars2 = series2.GetBars(startTime).Take(10).ToList();

		// Assert
		Assert.Equal(10, bars1.Count);
		Assert.Equal(10, bars2.Count);

		for (int i = 0; i < 10; i++)
		{
			Assert.Equal(bars1[i], bars2[i]);
		}
	}

	[Fact]
	public void GetBars_ConsistentWithGetBarAt_SameResults()
	{
		// Arrange
		const int seed = 12345;
		var interval = TimeSpan.FromMinutes(1);
		var startTime = new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc);

		var factory = new SeededBarSeries.Factory(seed);
		var series = factory.GetSeries(interval);

		// Act
		var barsFromSeries = series.GetBars(startTime).Take(5).ToList();
		var barsFromGetBarAt = new List<Bar>();
		for (int i = 0; i < 5; i++)
		{
			var timestamp = startTime.AddMinutes(i);
			barsFromGetBarAt.Add(series.GetBarAt(timestamp));
		}

		// Assert
		Assert.Equal(5, barsFromSeries.Count);
		Assert.Equal(5, barsFromGetBarAt.Count);

		for (int i = 0; i < 5; i++)
		{
			Assert.Equal(barsFromSeries[i], barsFromGetBarAt[i]);
		}
	}

	[Fact]
	public void GetBars_CorrectTimestampProgression_IncrementsByInterval()
	{
		// Arrange
		const int seed = 12345;
		var interval = TimeSpan.FromMinutes(5);
		var startTime = new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc);

		var factory = new SeededBarSeries.Factory(seed);
		var series = factory.GetSeries(interval);

		// Act
		var bars = series.GetBars(startTime).Take(10).ToList();

		// Assert
		for (int i = 0; i < 10; i++)
		{
			var expectedTimestamp = startTime.Add(TimeSpan.FromMinutes(i * 5));
			Assert.Equal(expectedTimestamp, bars[i].Timestamp);
		}
	}

	[Fact]
	public void GetBarAt_DifferentIntervals_SameSeed_ProducesDifferentResults()
	{
		// Arrange
		const int seed = 12345;
		var interval1 = TimeSpan.FromMinutes(1);
		var interval2 = TimeSpan.FromMinutes(5);
		var timestamp = new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc);

		var factory = new SeededBarSeries.Factory(seed);
		var series1 = factory.GetSeries(interval1);
		var series2 = factory.GetSeries(interval2);

		// Act
		var bar1 = series1.GetBarAt(timestamp);
		var bar2 = series2.GetBarAt(timestamp);

		// Assert
		Assert.NotEqual(bar1, bar2);
		Assert.Equal(timestamp, bar1.Timestamp);
		Assert.Equal(timestamp, bar2.Timestamp);
	}

	[Fact]
	public void GetBars_ValidOhlcRelationships_AllBarsValid()
	{
		// Arrange
		const int seed = 12345;
		var interval = TimeSpan.FromMinutes(1);
		var startTime = new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc);

		var factory = new SeededBarSeries.Factory(seed);
		var series = factory.GetSeries(interval);

		// Act
		var bars = series.GetBars(startTime).Take(100).ToList();

		// Assert
		Assert.All(bars, bar =>
		{
			Assert.True(bar.High >= bar.Open, $"High should be >= Open for {bar.Timestamp}");
			Assert.True(bar.High >= bar.Close, $"High should be >= Close for {bar.Timestamp}");
			Assert.True(bar.Low <= bar.Open, $"Low should be <= Open for {bar.Timestamp}");
			Assert.True(bar.Low <= bar.Close, $"Low should be <= Close for {bar.Timestamp}");
			Assert.True(bar.High >= bar.Low, $"High should be >= Low for {bar.Timestamp}");
			Assert.True(bar.Volume > 0, $"Volume should be positive for {bar.Timestamp}");
		});
	}

	[Fact]
	public void GetBarAt_CrossRunDeterminism_ReturnsExpectedValues()
	{
		// Arrange
		const int seed = 12345;
		var interval = TimeSpan.FromMinutes(1);
		var timestamp = new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc);

		var factory = new SeededBarSeries.Factory(seed);
		var series = factory.GetSeries(interval);

		// Act
		var bar = series.GetBarAt(timestamp);

		// Assert - These values should be consistent across all runs and platforms
		Assert.Equal(96.28m, Math.Round(bar.Open, 2));
		Assert.Equal(97.57m, Math.Round(bar.High, 2));
		Assert.Equal(95.48m, Math.Round(bar.Low, 2));
		Assert.Equal(95.48m, Math.Round(bar.Close, 2));
		Assert.Equal(7777m, bar.Volume);
		Assert.Equal(timestamp, bar.Timestamp);
	}

	[Fact]
	public void GetBars_MultipleBarsDeterminism_ReturnsExpectedSequence()
	{
		// Arrange
		const int seed = 12345;
		var interval = TimeSpan.FromMinutes(1);
		var startTime = new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc);

		var factory = new SeededBarSeries.Factory(seed);
		var series = factory.GetSeries(interval);

		// Act
		var bars = series.GetBars(startTime).Take(3).ToList();

		// Assert - These values should be consistent across all runs and platforms
		var bar0 = bars[0];
		Assert.Equal(96.28m, Math.Round(bar0.Open, 2));
		Assert.Equal(7777m, bar0.Volume);

		var bar1 = bars[1];
		Assert.Equal(96.21m, Math.Round(bar1.Open, 2));
		Assert.Equal(6786m, bar1.Volume);

		var bar2 = bars[2];
		Assert.Equal(96.13m, Math.Round(bar2.Open, 2));
		Assert.Equal(8952m, bar2.Volume);
	}

	[Theory]
	[InlineData(1)]
	[InlineData(5)]
	[InlineData(15)]
	[InlineData(60)]
	[InlineData(240)]
	[InlineData(1440)] // Daily
	public void GetSeries_DifferentIntervals_AllWork(int intervalMinutes)
	{
		// Arrange
		const int seed = 12345;
		var interval = TimeSpan.FromMinutes(intervalMinutes);
		var timestamp = new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc);

		var factory = new SeededBarSeries.Factory(seed);
		var series = factory.GetSeries(interval);

		// Act
		var bar = series.GetBarAt(timestamp);
		var bars = series.GetBars(timestamp).Take(5).ToList();

		// Assert
		Assert.Equal(timestamp, bar.Timestamp);
		Assert.Equal(5, bars.Count);
		Assert.All(bars, b => Assert.True(b.High >= b.Low));
		Assert.All(bars, b => Assert.True(b.Volume > 0));

		// Check timestamp progression
		for (int i = 1; i < bars.Count; i++)
		{
			var expectedInterval = TimeSpan.FromMinutes(intervalMinutes);
			var actualInterval = bars[i].Timestamp - bars[i - 1].Timestamp;
			Assert.Equal(expectedInterval, actualInterval);
		}
	}
}
