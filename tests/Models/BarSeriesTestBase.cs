using Illusionist.Core;

namespace Illusionist.Tests.Models;

/// <summary>
/// Base class for testing IBarSeriesFactory implementations to ensure deterministic behavior
/// and correct bar series generation. Inheritors only need to implement CreateFactory.
/// </summary>
public abstract class BarSeriesTestBase
{
    /// <summary>
    /// Creates an appropriate IBarSeriesFactory implementation with the given seed.
    /// </summary>
    /// <param name="seed">The seed for deterministic generation</param>
    /// <returns>An instance of IBarSeriesFactory</returns>
    protected abstract IBarSeriesFactory CreateFactory(int seed);
    
    /// <summary>
    /// Creates an appropriate IBarSeriesFactory implementation with additional parameters.
    /// Used for testing parameter variations.
    /// </summary>
    /// <param name="seed">The seed for deterministic generation</param>
    /// <param name="parameters">Additional parameters specific to the factory type</param>
    /// <returns>An instance of IBarSeriesFactory</returns>
    protected virtual IBarSeriesFactory CreateFactoryWithParameters(int seed, object parameters) 
        => CreateFactory(seed);    /// <summary>
    /// Creates a default BarInterval for testing (1 minute).
    /// </summary>
    protected static BarInterval CreateDefaultInterval() => BarInterval.Minute(1);

    /// <summary>
    /// Creates a default ISchedule for testing (1 minute).
    /// </summary>
    protected static ISchedule CreateDefaultSchedule() => CreateScheduleFromInterval(CreateDefaultInterval());

    /// <summary>
    /// Creates an ISchedule from a BarInterval using the DefaultEquitiesScheduleFactory.
    /// </summary>
    protected static ISchedule CreateScheduleFromInterval(BarInterval interval)
    {
        var factory = new DefaultEquitiesScheduleFactory();
        return factory.GetSchedule(interval);
    }
    
    /// <summary>
    /// Creates a BarInterval from minutes for testing convenience.
    /// </summary>
    protected static BarInterval CreateInterval(int minutes) => BarInterval.Minute((ushort)minutes);
    
    /// <summary>
    /// Creates a default BarAnchor for testing.
    /// </summary>
    protected static BarAnchor CreateDefaultAnchor() => new(new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc), 100.0m);

    [Fact]
    public void GetBarAt_SameSeedAndTimestamp_ReturnsIdenticalBars()
    {        // Arrange
        const int seed = 12345;
        var schedule = CreateDefaultSchedule();
        var anchor = CreateDefaultAnchor();
        var timestamp = new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc);

        var factory1 = CreateFactory(seed);
        var factory2 = CreateFactory(seed);
        var series1 = factory1.GetSeries(schedule, anchor);
        var series2 = factory2.GetSeries(schedule, anchor);

        // Act
        var bar1 = series1.GetBarAt(timestamp);
        var bar2 = series2.GetBarAt(timestamp);

        // Assert
        Assert.Equal(bar1, bar2);
    }

    [Fact]
    public void GetBarAt_DifferentSeeds_ReturnsDifferentBars()
    {        // Arrange
        const int seed1 = 12345;
        const int seed2 = 54321;
        var schedule = CreateDefaultSchedule();
        var anchor = CreateDefaultAnchor();
        var timestamp = new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc);

        var factory1 = CreateFactory(seed1);
        var factory2 = CreateFactory(seed2);
        var series1 = factory1.GetSeries(schedule, anchor);
        var series2 = factory2.GetSeries(schedule, anchor);

        // Act
        var bar1 = series1.GetBarAt(timestamp);
        var bar2 = series2.GetBarAt(timestamp);

        // Assert
        Assert.NotEqual(bar1, bar2);
    }

    [Fact]
    public void GetBars_MultipleCalls_ReturnsIdenticalSequences()
    {        // Arrange
        const int seed = 12345;
        var interval = CreateInterval(5);
        var schedule = CreateScheduleFromInterval(interval);
        var anchor = CreateDefaultAnchor();
        var startTime = new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc);

        var factory = CreateFactory(seed);
        var series1 = factory.GetSeries(schedule, anchor);
        var series2 = factory.GetSeries(schedule, anchor);

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
    }    [Fact]    public void GetBars_ConsistentWithGetBarAt_SameResults()
    {
        // Arrange
        const int seed = 12345;
        var interval = CreateDefaultInterval();
        var anchor = CreateDefaultAnchor();
        var startTime = new DateTime(2025, 1, 3, 9, 30, 0, DateTimeKind.Utc); // Friday, market open

        var factory = CreateFactory(seed);
        var schedule = CreateScheduleFromInterval(interval);
        var series = factory.GetSeries(schedule, anchor);        // Act
        var barsFromSeries = series.GetBars(startTime).Take(5).ToList();
        var barsFromGetBarAt = new List<Bar>();
        
        // Use the actual timestamps from the series instead of simple minute progression
        for (int i = 0; i < 5; i++)
        {
            barsFromGetBarAt.Add(series.GetBarAt(barsFromSeries[i].Timestamp));
        }

        // Assert
        Assert.Equal(5, barsFromSeries.Count);
        Assert.Equal(5, barsFromGetBarAt.Count);

        for (int i = 0; i < 5; i++)
        {
            Assert.Equal(barsFromSeries[i], barsFromGetBarAt[i]);
        }
    }    [Fact]    public void GetBars_CorrectTimestampProgression_IncrementsByInterval()
    {
        // Arrange
        const int seed = 12345;
        var interval = CreateInterval(5);
        var anchor = CreateDefaultAnchor();
        var startTime = new DateTime(2025, 1, 3, 9, 30, 0, DateTimeKind.Utc); // Friday, market open

        var factory = CreateFactory(seed);
        var schedule = CreateScheduleFromInterval(interval);
        var series = factory.GetSeries(schedule, anchor);

        // Act
        var bars = series.GetBars(startTime).Take(10).ToList();        // Assert
        for (int i = 0; i < 10; i++)
        {
            // Calculate expected timestamp using the schedule
            var expectedTimestamp = i == 0 ? startTime : schedule.GetNextValidBarTime(bars[i - 1].Timestamp);
            Assert.Equal(expectedTimestamp, bars[i].Timestamp);
        }
    }

    [Fact]
    public void GetBarAt_DifferentIntervals_SameSeed_ProducesDifferentResults()
    {
        // Arrange
        const int seed = 12345;
        var interval1 = CreateDefaultInterval();        var interval2 = CreateInterval(5);
        var anchor = CreateDefaultAnchor();
        var timestamp = new DateTime(2025, 1, 3, 9, 30, 0, DateTimeKind.Utc); // Friday, market open

        var factory = CreateFactory(seed);
        var schedule1 = CreateScheduleFromInterval(interval1);
        var schedule2 = CreateScheduleFromInterval(interval2);
        var series1 = factory.GetSeries(schedule1, anchor);
        var series2 = factory.GetSeries(schedule2, anchor);

        // Act
        var bar1 = series1.GetBarAt(timestamp);
        var bar2 = series2.GetBarAt(timestamp);

        // Assert
        Assert.NotEqual(bar1, bar2);
        Assert.Equal(timestamp, bar1.Timestamp);
        Assert.Equal(timestamp, bar2.Timestamp);
    }    [Fact]
    public void GetBars_ValidOhlcRelationships_AllBarsValid()
    {
        // Arrange
        const int seed = 12345;
        var interval = CreateDefaultInterval();
        var anchor = CreateDefaultAnchor();
        var startTime = new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc);

        var factory = CreateFactory(seed);
        var schedule = CreateScheduleFromInterval(interval);
        var series = factory.GetSeries(schedule, anchor);

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
        var interval = CreateInterval(intervalMinutes);
        var anchor = CreateDefaultAnchor();        var timestamp = new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc);

        var factory = CreateFactory(seed);
        var schedule = CreateScheduleFromInterval(interval);
        var series = factory.GetSeries(schedule, anchor);

        // Act
        var bar = series.GetBarAt(timestamp);
        var bars = series.GetBars(timestamp).Take(5).ToList();

        // Assert
        Assert.Equal(timestamp, bar.Timestamp);
        Assert.Equal(5, bars.Count);
        Assert.All(bars, b => Assert.True(b.High >= b.Low));
        Assert.All(bars, b => Assert.True(b.Volume > 0));        // Check timestamp progression - with schedule-aware advancement
        for (int i = 1; i < bars.Count; i++)
        {
            // With market hours, intervals may not be exactly the configured interval
            // due to market close/open transitions, so we just verify progression is forward
            Assert.True(bars[i].Timestamp > bars[i - 1].Timestamp, 
                $"Timestamp should progress forward: {bars[i - 1].Timestamp} < {bars[i].Timestamp}");
        }
    }
}
