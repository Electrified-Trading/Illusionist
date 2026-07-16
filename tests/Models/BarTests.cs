namespace Illusionist.Tests.Models;

/// <summary>
/// Tests for <see cref="Bar{T}"/> over <see cref="OHLC"/> to validate its immutable properties and behavior.
/// </summary>
public class BarTests
{
	private static Bar<OHLC> CreateBar(DateTime timestamp, decimal open, decimal high, decimal low, decimal close, decimal volume)
		=> Bar.Create(timestamp, new OHLC { Open = open, High = high, Low = low, Close = close }, (long)volume);

	[Fact]
	public void Bar_Constructor_ShouldCreateValidInstance()
	{
		// Arrange
		var timestamp = new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc);
		var open = 100.50m;
		var high = 101.25m;
		var low = 99.75m;
		var close = 100.85m;
		var volume = 1500m;

		// Act
		var bar = CreateBar(timestamp, open, high, low, close, volume);

		// Assert
		Assert.Equal(timestamp, bar.Timestamp);
		Assert.Equal(open, bar.Data.Open);
		Assert.Equal(high, bar.Data.High);
		Assert.Equal(low, bar.Data.Low);
		Assert.Equal(close, bar.Data.Close);
		Assert.Equal(volume, bar.Volume);
	}

	[Fact]
	public void Bar_Equality_ShouldWorkCorrectly()
	{
		// Arrange
		var timestamp = new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc);
		var bar1 = CreateBar(timestamp, 100m, 101m, 99m, 100.5m, 1000m);
		var bar2 = CreateBar(timestamp, 100m, 101m, 99m, 100.5m, 1000m);
		var bar3 = CreateBar(timestamp, 100m, 101m, 99m, 100.6m, 1000m); // Different close

		// Act & Assert
		Assert.Equal(bar1, bar2);
		Assert.NotEqual(bar1, bar3);
		Assert.True(bar1 == bar2);
		Assert.False(bar1 == bar3);
	}

	[Fact]
	public void Bar_HashCode_ShouldBeConsistent()
	{
		// Arrange
		var timestamp = new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc);
		var bar1 = CreateBar(timestamp, 100m, 101m, 99m, 100.5m, 1000m);
		var bar2 = CreateBar(timestamp, 100m, 101m, 99m, 100.5m, 1000m);

		// Act & Assert
		Assert.Equal(bar1.GetHashCode(), bar2.GetHashCode());
	}

	[Theory]
	[InlineData(100, 101, 99, 100.5)] // Valid OHLC
	[InlineData(50.25, 52.75, 49.50, 51.00)] // Valid OHLC with decimals
	public void Bar_ValidOhlcData_ShouldBeAccepted(decimal open, decimal high, decimal low, decimal close)
	{
		// Arrange
		var timestamp = DateTime.UtcNow;
		var volume = 1000m;

		// Act
		var bar = CreateBar(timestamp, open, high, low, close, volume);

		// Assert
		Assert.Equal(open, bar.Data.Open);
		Assert.Equal(high, bar.Data.High);
		Assert.Equal(low, bar.Data.Low);
		Assert.Equal(close, bar.Data.Close);

		// Verify OHLC constraints (these would typically be enforced by the generator)
		Assert.True(bar.Data.High >= bar.Data.Open, "High should be >= Open");
		Assert.True(bar.Data.High >= bar.Data.Close, "High should be >= Close");
		Assert.True(bar.Data.Low <= bar.Data.Open, "Low should be <= Open");
		Assert.True(bar.Data.Low <= bar.Data.Close, "Low should be <= Close");
	}
}
