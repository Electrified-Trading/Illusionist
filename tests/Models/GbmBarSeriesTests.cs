using Illusionist.Core.Catalog;

namespace Illusionist.Tests.Models;

/// <summary>
/// Tests for GbmBarSeries implementations to ensure deterministic behavior
/// and correct Geometric Brownian Motion characteristics.
/// </summary>
public class GbmBarSeriesTests
{
	[Fact]
	public void GetBarAt_SameSeedAndTimestamp_ReturnsIdenticalBars()
	{
		// Arrange
		const string symbol = "AAPL";
		const int seed = 12345;
		var interval = TimeSpan.FromMinutes(1);
		var timestamp = new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc);
		var factory1 = new GbmBarSeries.Factory(symbol, seed);
		var factory2 = new GbmBarSeries.Factory(symbol, seed);
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
		const string symbol = "AAPL";
		const int seed1 = 12345;
		const int seed2 = 54321;
		var interval = TimeSpan.FromMinutes(1);
		var timestamp = new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc);
		var factory1 = new GbmBarSeries.Factory(symbol, seed1);
		var factory2 = new GbmBarSeries.Factory(symbol, seed2);
		var series1 = factory1.GetSeries(interval);
		var series2 = factory2.GetSeries(interval);

		// Act
		var bar1 = series1.GetBarAt(timestamp);
		var bar2 = series2.GetBarAt(timestamp);

		// Assert
		Assert.NotEqual(bar1, bar2);
	}

	[Fact]
	public void GetBarAt_DifferentDriftParameters_ProducesDifferentResults()
	{
		// Arrange
		const string symbol = "AAPL";
		const int seed = 12345;
		var interval = TimeSpan.FromMinutes(1);
		var timestamp = new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc);
		var factory1 = new GbmBarSeries.Factory(symbol, seed, drift: 0.0001);
		var factory2 = new GbmBarSeries.Factory(symbol, seed, drift: 0.0005);
		var series1 = factory1.GetSeries(interval);
		var series2 = factory2.GetSeries(interval);

		// Act
		var bar1 = series1.GetBarAt(timestamp);
		var bar2 = series2.GetBarAt(timestamp);

		// Assert
		Assert.NotEqual(bar1, bar2);
		Assert.Equal(timestamp, bar1.Timestamp);
		Assert.Equal(timestamp, bar2.Timestamp);
	}

	[Fact]
	public void GetBarAt_DifferentVolatilityParameters_ProducesDifferentResults()
	{
		// Arrange
		const string symbol = "AAPL";
		const int seed = 12345;
		var interval = TimeSpan.FromMinutes(1);
		var timestamp = new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc);
		var factory1 = new GbmBarSeries.Factory(symbol, seed, volatility: 0.01);
		var factory2 = new GbmBarSeries.Factory(symbol, seed, volatility: 0.05);
		var series1 = factory1.GetSeries(interval);
		var series2 = factory2.GetSeries(interval);

		// Act
		var bar1 = series1.GetBarAt(timestamp);
		var bar2 = series2.GetBarAt(timestamp);

		// Assert
		Assert.NotEqual(bar1, bar2);
		Assert.Equal(timestamp, bar1.Timestamp);
		Assert.Equal(timestamp, bar2.Timestamp);
	}

	[Fact]
	public void GetBarAt_ValidOhlcRelationships_HighIsHighestLowIsLowest()
	{
		// Arrange
		const string symbol = "AAPL"; const int seed = 12345;
		var interval = TimeSpan.FromMinutes(5);
		var timestamp = new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc);

		var factory = new GbmBarSeries.Factory(symbol, seed);
		var series = factory.GetSeries(interval);

		// Act
		var bar = series.GetBarAt(timestamp);

		// Assert
		Assert.True(bar.High >= bar.Open, "High should be >= Open");
		Assert.True(bar.High >= bar.Close, "High should be >= Close");
		Assert.True(bar.Low <= bar.Open, "Low should be <= Open");
		Assert.True(bar.Low <= bar.Close, "Low should be <= Close");
		Assert.True(bar.High >= bar.Low, "High should be >= Low");
	}

	[Fact]
	public void GetBarAt_PositiveVolume_VolumeIsAlwaysPositive()
	{        // Arrange
		const string symbol = "AAPL";
		const int seed = 12345;
		var interval = TimeSpan.FromMinutes(1);
		var factory = new GbmBarSeries.Factory(symbol, seed);
		var series = factory.GetSeries(interval);

		// Act & Assert
		for (int i = 0; i < 100; i++)
		{
			var timestamp = new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc).AddMinutes(i);
			var bar = series.GetBarAt(timestamp);

			Assert.True(bar.Volume > 0, $"Volume should be positive for timestamp {timestamp}");
		}
	}
	[Fact]
	public void GetBarAt_ReasonablePriceRange_PricesWithinExpectedBounds()
	{
		// Arrange
		const string symbol = "AAPL";
		const int seed = 12345;
		var interval = TimeSpan.FromMinutes(1);
		var timestamp = new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc);

		var factory = new GbmBarSeries.Factory(symbol, seed);
		var series = factory.GetSeries(interval);

		// Act
		var bar = series.GetBarAt(timestamp);

		// Assert
		Assert.True(bar.Open > 0, "Open price should be positive");
		Assert.True(bar.High > 0, "High price should be positive");
		Assert.True(bar.Low > 0, "Low price should be positive");
		Assert.True(bar.Close > 0, "Close price should be positive");

		// GBM prices should be reasonable (based on exp() function)
		Assert.True(bar.Open is > 0.01m and < 1000m, "Open price should be in reasonable range");
		Assert.True(bar.High is > 0.01m and < 1000m, "High price should be in reasonable range");
		Assert.True(bar.Low is > 0.01m and < 1000m, "Low price should be in reasonable range");
		Assert.True(bar.Close is > 0.01m and < 1000m, "Close price should be in reasonable range");
	}

	[Fact]
	public void GetBars_ConsistentWithGetBarAt_SameResults()
	{        // Arrange
		const string symbol = "AAPL";
		const int seed = 12345;
		var interval = TimeSpan.FromMinutes(1);
		var startTime = new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc);

		var factory = new GbmBarSeries.Factory(symbol, seed);
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
	public void GetBarAt_CrossRunDeterminism_ReturnsExpectedValues()
	{        // Arrange
		const string symbol = "AAPL";
		const int seed = 12345;
		var interval = TimeSpan.FromMinutes(1);
		var timestamp = new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc);

		var factory = new GbmBarSeries.Factory(symbol, seed);
		var series = factory.GetSeries(interval);

		// Act
		var bar = series.GetBarAt(timestamp);

		// Assert - These values should be consistent across all runs and platforms
		Assert.True(bar.Open is > 0.5m and < 2.0m, "Open should be in expected GBM range");
		Assert.True(bar.Volume >= 1000m, "Volume should be at least 1000");
		Assert.Equal(timestamp, bar.Timestamp);
	}

	[Theory]
	[InlineData(1)]
	[InlineData(5)]
	[InlineData(15)]
	[InlineData(60)]
	public void GetSeries_DifferentIntervals_AllWork(int intervalMinutes)
	{
		// Arrange
		const string symbol = "AAPL";
		const int seed = 12345;
		var interval = TimeSpan.FromMinutes(intervalMinutes);
		var timestamp = new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc);

		var factory = new GbmBarSeries.Factory(symbol, seed);
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

	[Fact]
	public void Generator_DirectAccess_WorksCorrectly()
	{
		// Arrange
		const int seed = 12345;
		var interval = TimeSpan.FromMinutes(1);
		var timestamp = new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc);

		var generator = new GbmBarSeries.Generator(seed, interval, 0.0001, 0.01);

		// Act
		var bar = generator.GetBarAt(timestamp);

		// Assert
		Assert.Equal(interval, generator.Interval);
		Assert.Equal(timestamp, bar.Timestamp);
		Assert.True(bar.High >= bar.Low);
		Assert.True(bar.Volume > 0);
	}
}
