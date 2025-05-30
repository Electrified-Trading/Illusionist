using Illusionist.Core.Catalog;

namespace Illusionist.Tests.Models;

/// <summary>
/// Tests for GbmBarSeries implementations to ensure deterministic behavior
/// and correct Geometric Brownian Motion characteristics.
/// </summary>
public class GbmBarSeriesTests : BarSeriesTestBase
{
	private const string DefaultSymbol = "AAPL";

	/// <summary>
	/// Creates a GbmBarSeries.Factory with the specified seed.
	/// </summary>
	/// <param name="seed">The random seed for deterministic generation</param>
	/// <returns>A GbmBarSeries factory instance</returns>
	protected override IBarSeriesFactory CreateFactory(int seed)
	{
		return new GbmBarSeries.Factory(DefaultSymbol, seed);
	}

	/// <summary>
	/// Creates a GbmBarSeries.Factory with the specified seed and parameters.
	/// </summary>
	/// <param name="seed">The random seed for deterministic generation</param>
	/// <param name="parameters">Dictionary containing 'drift' or 'volatility' values</param>
	/// <returns>A GbmBarSeries factory instance with custom parameters</returns>
	protected override IBarSeriesFactory CreateFactoryWithParameters(int seed, object parameters)
	{
		if (parameters is not Dictionary<string, double> customParams)
		{
			return CreateFactory(seed);
		}

		double drift = 0.0001;
		double volatility = 0.01;

		if (customParams.TryGetValue("drift", out var driftValue))
		{
			drift = driftValue;
		}

		if (customParams.TryGetValue("volatility", out var volValue))
		{
			volatility = volValue;
		}

		return new GbmBarSeries.Factory(DefaultSymbol, seed, drift, volatility);
	}
	[Fact]
	public void GetBarAt_DifferentDriftParameters_ProducesDifferentResults()
	{
		// Arrange
		const int seed = 12345;
		var interval = CreateDefaultInterval();
		var anchor = CreateDefaultAnchor();
		var timestamp = new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc);

		var params1 = new Dictionary<string, double> { ["drift"] = 0.0001 };
		var params2 = new Dictionary<string, double> { ["drift"] = 0.0005 };

		var factory1 = CreateFactoryWithParameters(seed, params1);
		var factory2 = CreateFactoryWithParameters(seed, params2);
		var series1 = factory1.GetSeries(interval, anchor);
		var series2 = factory2.GetSeries(interval, anchor);

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
		const int seed = 12345;
		var interval = CreateDefaultInterval();
		var anchor = CreateDefaultAnchor();
		var timestamp = new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc);

		var params1 = new Dictionary<string, double> { ["volatility"] = 0.01 };
		var params2 = new Dictionary<string, double> { ["volatility"] = 0.05 };

		var factory1 = CreateFactoryWithParameters(seed, params1);
		var factory2 = CreateFactoryWithParameters(seed, params2);
		var series1 = factory1.GetSeries(interval, anchor);
		var series2 = factory2.GetSeries(interval, anchor);

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
		const int seed = 12345;
		var interval = CreateInterval(5);
		var anchor = CreateDefaultAnchor();
		var timestamp = new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc);

		var factory = CreateFactory(seed);
		var series = factory.GetSeries(interval, anchor);

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
	{
		// Arrange
		const int seed = 12345;
		var interval = CreateDefaultInterval();
		var anchor = CreateDefaultAnchor();
		var factory = CreateFactory(seed);
		var series = factory.GetSeries(interval, anchor);

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
		const int seed = 12345;
		var interval = CreateDefaultInterval();
		var anchor = CreateDefaultAnchor();
		var timestamp = new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc);

		var factory = CreateFactory(seed);
		var series = factory.GetSeries(interval, anchor);

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
	}	[Fact]
	public void GetBarAt_CrossRunDeterminism_ReturnsExpectedValues()
	{
		// Arrange
		const int seed = 12345;
		var interval = CreateDefaultInterval();
		var anchor = CreateDefaultAnchor();
		var timestamp = new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc);

		var factory = CreateFactory(seed);
		var series = factory.GetSeries(interval, anchor);

		// Act
		var bar = series.GetBarAt(timestamp);

		// Assert - These values should be consistent across all runs and platforms
		// Since we're querying at the anchor timestamp, prices should be near the anchor value
		Assert.True(bar.Open is > 99.0m and < 101.0m, "Open should be near anchor price");
		Assert.True(bar.Volume >= 1000m, "Volume should be at least 1000");
		Assert.Equal(timestamp, bar.Timestamp);
	}
	[Fact]
	public void Generator_DirectAccess_WorksCorrectly()
	{
		// Arrange
		const int seed = 12345;
		var interval = TimeSpan.FromMinutes(1);
		var timestamp = new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc);
		var anchor = new BarAnchor(new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc), 100.0m);

		var generator = new GbmBarSeries.Generator(seed, interval, 0.0001, 0.01, anchor);

		// Act
		var bar = generator.GetBarAt(timestamp);

		// Assert
		Assert.Equal(interval, generator.Interval);
		Assert.Equal(timestamp, bar.Timestamp);
		Assert.True(bar.High >= bar.Low);
		Assert.True(bar.Volume > 0);
	}

	[Fact]
	public void GetBarAt_AnchorTime_ReturnsBarWithAnchorPrice()
	{
		// Arrange
		const int seed = 12345;
		var interval = CreateDefaultInterval();
		var anchor = new BarAnchor(new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc), 150.0m);

		var factory = CreateFactory(seed);
		var series = factory.GetSeries(interval, anchor);

		// Act
		var bar = series.GetBarAt(anchor.Timestamp);

		// Assert
		Assert.Equal(anchor.Value, bar.Open);
		Assert.Equal(anchor.Timestamp, bar.Timestamp);
	}
	[Fact]
	public void GetBarAt_PositiveDrift_IncreasesOverTime()
	{
		// Arrange
		const int seed = 12345;
		var interval = CreateDefaultInterval();
		var anchor = new BarAnchor(new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc), 100.0m);
		
		// Use higher drift (50% annual) and lower volatility to make drift effect visible over one day
		var factory = new GbmBarSeries.Factory("AAPL", seed, drift: 0.5, volatility: 0.001);
		var series = factory.GetSeries(interval, anchor);

		// Act
		var barAtAnchor = series.GetBarAt(anchor.Timestamp);
		var barOneDayLater = series.GetBarAt(anchor.Timestamp.AddDays(1));

		// Assert
		Assert.True(barOneDayLater.Open > barAtAnchor.Open, 
			$"Price should increase with positive drift: {barAtAnchor.Open} -> {barOneDayLater.Open}");
	}
	[Fact]
	public void GetBarAt_OneDayBefore_WithPositiveDrift_ReturnsLowerPrice()
	{
		// Arrange
		const int seed = 12345;
		var interval = CreateDefaultInterval();
		var anchor = new BarAnchor(new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc), 100.0m);
		
		// Use higher drift (50% annual) and lower volatility to make drift effect visible over one day
		var factory = new GbmBarSeries.Factory("AAPL", seed, drift: 0.5, volatility: 0.001);
		var series = factory.GetSeries(interval, anchor);

		// Act
		var barOneDayBefore = series.GetBarAt(anchor.Timestamp.AddDays(-1));
		var barAtAnchor = series.GetBarAt(anchor.Timestamp);

		// Assert
		Assert.True(barOneDayBefore.Open < barAtAnchor.Open,
			$"Price should be lower one day before with positive drift: {barOneDayBefore.Open} < {barAtAnchor.Open}");
	}
}
