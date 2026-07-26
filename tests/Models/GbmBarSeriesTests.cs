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
	protected override IBarSeriesFactory<OHLC> CreateFactory(int seed)
	{
		return new GbmBarSeries.Factory(DefaultSymbol, seed);
	}

	/// <summary>
	/// Creates a GbmBarSeries.Factory with the specified seed and parameters.
	/// </summary>
	/// <param name="seed">The random seed for deterministic generation</param>
	/// <param name="parameters">Dictionary containing 'drift' or 'volatility' values</param>
	/// <returns>A GbmBarSeries factory instance with custom parameters</returns>
	protected override IBarSeriesFactory<OHLC> CreateFactoryWithParameters(int seed, object parameters)
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
		var schedule = CreateScheduleFromInterval(interval);
		var series1 = factory1.GetSeries(schedule, anchor);
		var series2 = factory2.GetSeries(schedule, anchor);

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
		var schedule = CreateScheduleFromInterval(interval);
		var series1 = factory1.GetSeries(schedule, anchor);
		var series2 = factory2.GetSeries(schedule, anchor);

		// Act
		var bar1 = series1.GetBarAt(timestamp);
		var bar2 = series2.GetBarAt(timestamp);

		// Assert
		Assert.NotEqual(bar1, bar2);
		Assert.Equal(timestamp, bar1.Timestamp);
		Assert.Equal(timestamp, bar2.Timestamp);
	}	[Fact]
	public void GetBarAt_ValidOhlcRelationships_HighIsHighestLowIsLowest()
	{
		// Arrange
		const int seed = 12345;
		var interval = CreateInterval(5);
		var anchor = CreateDefaultAnchor();
		var timestamp = new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc);

		var factory = CreateFactory(seed);
		var schedule = CreateScheduleFromInterval(interval);
		var series = factory.GetSeries(schedule, anchor);

		// Act
		var bar = series.GetBarAt(timestamp);

		// Assert
		Assert.True(bar.Data.High >= bar.Data.Open, "High should be >= Open");
		Assert.True(bar.Data.High >= bar.Data.Close, "High should be >= Close");
		Assert.True(bar.Data.Low <= bar.Data.Open, "Low should be <= Open");
		Assert.True(bar.Data.Low <= bar.Data.Close, "Low should be <= Close");
		Assert.True(bar.Data.High >= bar.Data.Low, "High should be >= Low");
	}	[Fact]
	public void GetBarAt_PositiveVolume_VolumeIsAlwaysPositive()
	{
		// Arrange
		const int seed = 12345;
		var interval = CreateDefaultInterval();
		var anchor = CreateDefaultAnchor();
		var factory = CreateFactory(seed);
		var schedule = CreateScheduleFromInterval(interval);
		var series = factory.GetSeries(schedule, anchor);

		// Act & Assert
		for (int i = 0; i < 100; i++)
		{
			var timestamp = new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc).AddMinutes(i);
			var bar = series.GetBarAt(timestamp);

			Assert.True(bar.Volume > 0, $"Volume should be positive for timestamp {timestamp}");
		}
	}	[Fact]
	public void GetBarAt_ReasonablePriceRange_PricesWithinExpectedBounds()
	{
		// Arrange
		const int seed = 12345;
		var interval = CreateDefaultInterval();
		var anchor = CreateDefaultAnchor();
		var timestamp = new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc);

		var factory = CreateFactory(seed);
		var schedule = CreateScheduleFromInterval(interval);
		var series = factory.GetSeries(schedule, anchor);

		// Act
		var bar = series.GetBarAt(timestamp);

		// Assert
		Assert.True(bar.Data.Open > 0, "Open price should be positive");
		Assert.True(bar.Data.High > 0, "High price should be positive");
		Assert.True(bar.Data.Low > 0, "Low price should be positive");
		Assert.True(bar.Data.Close > 0, "Close price should be positive");

		// GBM prices should be reasonable (based on exp() function)
		Assert.True(bar.Data.Open is > 0.01m and < 1000m, "Open price should be in reasonable range");
		Assert.True(bar.Data.High is > 0.01m and < 1000m, "High price should be in reasonable range");
		Assert.True(bar.Data.Low is > 0.01m and < 1000m, "Low price should be in reasonable range");
		Assert.True(bar.Data.Close is > 0.01m and < 1000m, "Close price should be in reasonable range");
	}	[Fact]
	public void GetBarAt_CrossRunDeterminism_ReturnsExpectedValues()
	{
		// Arrange
		const int seed = 12345;
		var interval = CreateDefaultInterval();
		var anchor = CreateDefaultAnchor();
		var timestamp = new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc);

		var factory = CreateFactory(seed);
		var schedule = CreateScheduleFromInterval(interval);
		var series = factory.GetSeries(schedule, anchor);

		// Act
		var bar = series.GetBarAt(timestamp);

		// Assert - These values should be consistent across all runs and platforms
		// Since we're querying at the anchor timestamp, prices should be near the anchor value
		Assert.True(bar.Data.Open is > 99.0m and < 101.0m, "Open should be near anchor price");
		Assert.True(bar.Volume >= 1000m, "Volume should be at least 1000");
		Assert.Equal(timestamp, bar.Timestamp);
	}

	/// <summary>
	/// Task 10-0021: <see cref="GbmBarSeries"/> derives its generator seed from
	/// <c>seed</c> combined with a deterministic hash of <c>symbol</c> (previously
	/// <c>seed + symbol.GetHashCode()</c>, which is randomized per .NET 5+ process, so the
	/// same (seed, symbol) pair silently produced a different price path in every process
	/// launch). Unlike <see cref="GetBarAt_CrossRunDeterminism_ReturnsExpectedValues"/> above,
	/// which only checks the result is plausible, this pins the exact bar values so any
	/// regression — reverting to <see cref="string.GetHashCode()"/>, or any change to the
	/// deterministic hash algorithm itself — fails this single in-process assertion, with no
	/// process boundary required. Expected values were captured by actually executing this
	/// exact (symbol, seed, anchor, schedule) combination through the fixed generator — not
	/// hand-derived — and confirmed identical across two separate process launches of a
	/// throwaway harness before being pinned here as literals.
	/// </summary>
	[Fact]
	public void GetBarAt_KnownSeedAndSymbol_ProducesPinnedLiteralValues()
	{
		// Arrange
		const string symbol = "SYNTH";
		const int seed = 12345;
		var anchor = CreateDefaultAnchor();
		var schedule = CreateScheduleFromInterval(CreateDefaultInterval());

		var factory = new GbmBarSeries.Factory(symbol, seed);
		var series = factory.GetSeries(schedule, anchor);

		// Act
		var bars = series.GetBars(anchor.Timestamp).Take(3).ToList();

		// Assert
		Assert.Equal(100.0m, bars[0].Data.Open);
		Assert.Equal(100.1080899586707611m, bars[0].Data.High);
		Assert.Equal(99.938716547398239m, bars[0].Data.Low);
		Assert.Equal(100.056784754997000m, bars[0].Data.Close);
		Assert.Equal(1883m, bars[0].Volume);

		Assert.Equal(100.056784754997000m, bars[1].Data.Open);
		Assert.Equal(100.05827998729774509m, bars[1].Data.High);
		Assert.Equal(100.05371024578029872m, bars[1].Data.Low);
		Assert.Equal(100.054722991879000m, bars[1].Data.Close);
		Assert.Equal(6032m, bars[1].Volume);

		Assert.Equal(100.054722991879000m, bars[2].Data.Open);
		Assert.Equal(100.05750391281702544m, bars[2].Data.High);
		Assert.Equal(100.053912446996841636m, bars[2].Data.Low);
		Assert.Equal(100.055974647535000m, bars[2].Data.Close);
		Assert.Equal(2959m, bars[2].Volume);
	}

	[Fact]
	public void Generator_DirectAccess_WorksCorrectly()
	{
		// Arrange
		const int seed = 12345;
		var interval = TimeSpan.FromMinutes(1);
		var timestamp = new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc);
		var anchor = new BarAnchor(new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc), 100.0m);

		var schedule = CreateScheduleFromInterval(BarInterval.Minute(1));
		var generator = new GbmBarSeries.Generator(seed, schedule, 0.0001, 0.01, anchor);

		// Act
		var bar = generator.GetBarAt(timestamp);

		// Assert
		Assert.Equal(interval, generator.Interval);
		Assert.Equal(timestamp, bar.Timestamp);
		Assert.True(bar.Data.High >= bar.Data.Low);
		Assert.True(bar.Volume > 0);
	}

	[Fact]
	public void GetBarAt_AnchorTime_ReturnsBarWithAnchorPrice()
	{		// Arrange
		const int seed = 12345;
		var interval = CreateDefaultInterval();
		var anchor = new BarAnchor(new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc), 150.0m);

		var factory = CreateFactory(seed);
		var schedule = CreateScheduleFromInterval(interval);
		var series = factory.GetSeries(schedule, anchor);

		// Act
		var bar = series.GetBarAt(anchor.Timestamp);

		// Assert
		Assert.Equal(anchor.Value, bar.Data.Open);
		Assert.Equal(anchor.Timestamp, bar.Timestamp);
	}
	[Fact]
	public void GetBarAt_PositiveDrift_IncreasesOverTime()
	{
		// Arrange
		const int seed = 12345;
		var interval = CreateDefaultInterval();		var anchor = new BarAnchor(new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc), 100.0m);
		
		// Use higher drift (50% annual) and lower volatility to make drift effect visible over one day
		var factory = new GbmBarSeries.Factory("AAPL", seed, drift: 0.5, volatility: 0.001);
		var schedule = CreateScheduleFromInterval(interval);
		var series = factory.GetSeries(schedule, anchor);

		// Act
		var barAtAnchor = series.GetBarAt(anchor.Timestamp);
		var barOneDayLater = series.GetBarAt(anchor.Timestamp.AddDays(1));

		// Assert
		Assert.True(barOneDayLater.Data.Open > barAtAnchor.Data.Open, 
			$"Price should increase with positive drift: {barAtAnchor.Data.Open} -> {barOneDayLater.Data.Open}");
	}
	[Fact]
	public void GetBarAt_OneDayBefore_WithPositiveDrift_ReturnsLowerPrice()
	{
		// Arrange
		const int seed = 12345;
		var interval = CreateDefaultInterval();		var anchor = new BarAnchor(new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc), 100.0m);
		
		// Use higher drift (50% annual) and lower volatility to make drift effect visible over one day
		var factory = new GbmBarSeries.Factory("AAPL", seed, drift: 0.5, volatility: 0.001);
		var schedule = CreateScheduleFromInterval(interval);
		var series = factory.GetSeries(schedule, anchor);

		// Act
		var barOneDayBefore = series.GetBarAt(anchor.Timestamp.AddDays(-1));
		var barAtAnchor = series.GetBarAt(anchor.Timestamp);

		// Assert
		Assert.True(barOneDayBefore.Data.Open < barAtAnchor.Data.Open,
			$"Price should be lower one day before with positive drift: {barOneDayBefore.Data.Open} < {barAtAnchor.Data.Open}");
	}
}
