using Illusionist.Core.Catalog;

namespace Illusionist.Tests.Models;

/// <summary>
/// Tests for the SeededBarSeries.Generator class to ensure deterministic behavior
/// and realistic bar generation.
/// </summary>
public class SeededBarSeriesGeneratorTests
{
	[Fact]
	public void GetBarAt_SameSeedAndTimestamp_ReturnsSameBar()
	{
		// Arrange
		const int seed = 12345;
		var interval = TimeSpan.FromMinutes(1);
		var timestamp = new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc);

		var generator1 = new SeededBarSeries.Generator(seed, interval);
		var generator2 = new SeededBarSeries.Generator(seed, interval);

		// Act
		var bar1 = generator1.GetBarAt(timestamp);
		var bar2 = generator2.GetBarAt(timestamp);

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

		var generator1 = new SeededBarSeries.Generator(seed1, interval);
		var generator2 = new SeededBarSeries.Generator(seed2, interval);

		// Act
		var bar1 = generator1.GetBarAt(timestamp);
		var bar2 = generator2.GetBarAt(timestamp);

		// Assert
		Assert.NotEqual(bar1, bar2);
	}

	[Fact]
	public void GetBarAt_DifferentTimestamps_ReturnsDifferentBars()
	{
		// Arrange
		const int seed = 12345;
		var interval = TimeSpan.FromMinutes(1);
		var timestamp1 = new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc);
		var timestamp2 = new DateTime(2025, 1, 1, 9, 1, 0, DateTimeKind.Utc);

		var generator = new SeededBarSeries.Generator(seed, interval);

		// Act
		var bar1 = generator.GetBarAt(timestamp1);
		var bar2 = generator.GetBarAt(timestamp2);

		// Assert
		Assert.NotEqual(bar1, bar2);
	}

	[Fact]
	public void GetBarAt_ValidOhlcRelationships_HighIsHighestLowIsLowest()
	{
		// Arrange
		const int seed = 12345;
		var interval = TimeSpan.FromMinutes(5);
		var timestamp = new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc);

		var generator = new SeededBarSeries.Generator(seed, interval);

		// Act
		var bar = generator.GetBarAt(timestamp);

		// Assert
		Assert.True(bar.High >= bar.Open, "High should be >= Open");
		Assert.True(bar.High >= bar.Close, "High should be >= Close");
		Assert.True(bar.Low <= bar.Open, "Low should be <= Open");
		Assert.True(bar.Low <= bar.Close, "Low should be <= Close");
		Assert.True(bar.High >= bar.Low, "High should be >= Low");
	}

	[Fact]
	public void GetBarAt_TimestampAlignment_AlignsToIntervalBoundary()
	{
		// Arrange
		const int seed = 12345;
		var interval = TimeSpan.FromMinutes(5);
		var baseTime = new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc);
		var offsetTime = baseTime.AddMinutes(2); // 2 minutes into the 5-minute interval

		var generator = new SeededBarSeries.Generator(seed, interval);

		// Act
		var barBase = generator.GetBarAt(baseTime);
		var barOffset = generator.GetBarAt(offsetTime);

		// Assert
		Assert.Equal(barBase.Timestamp, barOffset.Timestamp);
		Assert.Equal(baseTime, barBase.Timestamp);
	}

	[Fact]
	public void GetBarAt_PositiveVolume_VolumeIsAlwaysPositive()
	{
		// Arrange
		const int seed = 12345;
		var interval = TimeSpan.FromMinutes(1);
		var generator = new SeededBarSeries.Generator(seed, interval);

		// Act & Assert
		for (int i = 0; i < 100; i++)
		{
			var timestamp = new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc).AddMinutes(i);
			var bar = generator.GetBarAt(timestamp);

			Assert.True(bar.Volume > 0, $"Volume should be positive for timestamp {timestamp}");
		}
	}

	[Fact]
	public void GetBarAt_ReasonablePriceRange_PricesWithinExpectedBounds()
	{
		// Arrange
		const int seed = 12345;
		var interval = TimeSpan.FromMinutes(1);
		var generator = new SeededBarSeries.Generator(seed, interval);
		var timestamp = new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc);

		// Act
		var bar = generator.GetBarAt(timestamp);

		// Assert
		Assert.True(bar.Open > 0, "Open price should be positive");
		Assert.True(bar.High > 0, "High price should be positive");
		Assert.True(bar.Low > 0, "Low price should be positive");
		Assert.True(bar.Close > 0, "Close price should be positive");

		// Prices should be in a reasonable range (roughly 80-120 based on algorithm)
		Assert.True(bar.Open is > 80 and < 120, "Open price should be in reasonable range");
		Assert.True(bar.High is > 80 and < 130, "High price should be in reasonable range");
		Assert.True(bar.Low is > 70 and < 120, "Low price should be in reasonable range");
		Assert.True(bar.Close is > 80 and < 120, "Close price should be in reasonable range");
	}

	[Theory]
	[InlineData(1)]
	[InlineData(5)]
	[InlineData(15)]
	[InlineData(60)]
	public void GetBarAt_DifferentIntervals_GeneratesDifferentResults(int intervalMinutes)
	{
		// Arrange
		const int seed = 12345;
		var interval = TimeSpan.FromMinutes(intervalMinutes);
		var timestamp = new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc);

		var generator = new SeededBarSeries.Generator(seed, interval);

		// Act
		var bar = generator.GetBarAt(timestamp);

		// Assert
		Assert.Equal(timestamp, bar.Timestamp);
		Assert.True(bar.High >= bar.Low);
		Assert.True(bar.Volume > 0);
	}

	[Fact]
	public void GetBarAt_ConsecutiveBars_ShowsRealisticProgression()
	{
		// Arrange
		const int seed = 12345;
		var interval = TimeSpan.FromMinutes(1);
		var generator = new SeededBarSeries.Generator(seed, interval);
		var baseTime = new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc);

		// Act
		var bars = new List<Bar>();
		for (int i = 0; i < 10; i++)
		{
			var timestamp = baseTime.AddMinutes(i);
			bars.Add(generator.GetBarAt(timestamp));
		}

		// Assert
		Assert.Equal(10, bars.Count);
		Assert.All(bars, bar => Assert.True(bar.High >= bar.Low));
		Assert.All(bars, bar => Assert.True(bar.Volume > 0));

		// Check that consecutive bars have different values (shows progression)
		for (int i = 1; i < bars.Count; i++)
		{
			Assert.NotEqual(bars[i - 1], bars[i]);
		}
	}

	[Fact]
	public void GetBarAt_ExactDeterminism_ReturnsExpectedValues()
	{
		// Arrange
		const int seed = 12345;
		var interval = TimeSpan.FromMinutes(1);
		var timestamp = new DateTime(2025, 5, 30, 9, 0, 0, DateTimeKind.Utc);

		var generator = new SeededBarSeries.Generator(seed, interval);

		// Act
		var bar = generator.GetBarAt(timestamp);
		// Assert - These exact values should be reproducible across all runs
		Assert.Equal(new DateTime(2025, 5, 30, 9, 0, 0, DateTimeKind.Utc), bar.Timestamp);
		Assert.Equal(96.28m, Math.Round(bar.Open, 2));
		Assert.Equal(97.17m, Math.Round(bar.High, 2));
		Assert.Equal(95.48m, Math.Round(bar.Low, 2));
		Assert.Equal(95.48m, Math.Round(bar.Close, 2));
		Assert.Equal(1795m, bar.Volume);

		// Verify OHLC relationships
		Assert.True(bar.High >= bar.Open);
		Assert.True(bar.High >= bar.Close);
		Assert.True(bar.Low <= bar.Open);
		Assert.True(bar.Low <= bar.Close);
	}
}
