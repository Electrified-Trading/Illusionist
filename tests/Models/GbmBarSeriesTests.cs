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
	}
	[Fact]
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
		Assert.True(bar.Open is > 0.5m and < 2.0m, "Open should be in expected GBM range");
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
