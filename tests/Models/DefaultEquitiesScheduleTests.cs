namespace Illusionist.Tests.Models;

/// <summary>
/// Tests for <see cref="DefaultEquitiesSchedule"/> to verify market hours,
/// holiday handling, and proper time advancement.
/// </summary>
public sealed class DefaultEquitiesScheduleTests
{
    [Fact]
    public void IsValidBarTime_MarketHours_ReturnsTrue()
    {
        // Arrange
        var schedule = new DefaultEquitiesSchedule(BarInterval.Minute(1));
        var marketTime = new DateTime(2025, 1, 2, 10, 30, 0); // Thursday 10:30 AM

        // Act
        var result = schedule.IsValidBarTime(marketTime);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidBarTime_BeforeMarketOpen_ReturnsFalse()
    {
        // Arrange
        var schedule = new DefaultEquitiesSchedule(BarInterval.Minute(1));
        var earlyTime = new DateTime(2025, 1, 2, 9, 0, 0); // Thursday 9:00 AM

        // Act
        var result = schedule.IsValidBarTime(earlyTime);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidBarTime_AfterMarketClose_ReturnsFalse()
    {
        // Arrange
        var schedule = new DefaultEquitiesSchedule(BarInterval.Minute(1));
        var lateTime = new DateTime(2025, 1, 2, 16, 30, 0); // Thursday 4:30 PM

        // Act
        var result = schedule.IsValidBarTime(lateTime);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData(DayOfWeek.Saturday)]
    [InlineData(DayOfWeek.Sunday)]
    public void IsValidBarTime_Weekend_ReturnsFalse(DayOfWeek dayOfWeek)
    {
        // Arrange
        var schedule = new DefaultEquitiesSchedule(BarInterval.Minute(1));
        var weekendDate = GetDateForDayOfWeek(2025, 1, dayOfWeek);
        var weekendTime = weekendDate.Add(new TimeSpan(10, 30, 0)); // 10:30 AM

        // Act
        var result = schedule.IsValidBarTime(weekendTime);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData(2025, 1, 1)]   // New Year's Day
    [InlineData(2025, 7, 4)]   // Independence Day
    [InlineData(2025, 12, 25)] // Christmas Day
    public void IsValidBarTime_Holiday_ReturnsFalse(int year, int month, int day)
    {
        // Arrange
        var schedule = new DefaultEquitiesSchedule(BarInterval.Minute(1));
        var holiday = new DateTime(year, month, day, 10, 30, 0); // 10:30 AM on holiday

        // Act
        var result = schedule.IsValidBarTime(holiday);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetNextValidBarTime_WithinMarketHours_AdvancesByInterval()
    {
        // Arrange
        var schedule = new DefaultEquitiesSchedule(BarInterval.Minute(5));
        var currentTime = new DateTime(2025, 1, 2, 10, 30, 0); // Thursday 10:30 AM

        // Act
        var nextTime = schedule.GetNextValidBarTime(currentTime);

        // Assert
        var expected = new DateTime(2025, 1, 2, 10, 35, 0); // Thursday 10:35 AM
        Assert.Equal(expected, nextTime);
    }

    [Fact]
    public void GetNextValidBarTime_AfterMarketClose_JumpsToNextTradingDay()
    {
        // Arrange
        var schedule = new DefaultEquitiesSchedule(BarInterval.Minute(5));
        var afterClose = new DateTime(2025, 1, 2, 16, 30, 0); // Thursday 4:30 PM

        // Act
        var nextTime = schedule.GetNextValidBarTime(afterClose);

        // Assert
        var expected = new DateTime(2025, 1, 3, 9, 30, 0); // Friday 9:30 AM
        Assert.Equal(expected, nextTime);
    }

    [Fact]
    public void GetNextValidBarTime_Friday_SkipsWeekendToMonday()
    {
        // Arrange
        var schedule = new DefaultEquitiesSchedule(BarInterval.Minute(5));
        var fridayAfterClose = new DateTime(2025, 1, 3, 16, 30, 0); // Friday 4:30 PM

        // Act
        var nextTime = schedule.GetNextValidBarTime(fridayAfterClose);

        // Assert
        var expected = new DateTime(2025, 1, 6, 9, 30, 0); // Monday 9:30 AM
        Assert.Equal(expected, nextTime);
    }

    [Fact]
    public void GetNextValidBarTime_BeforeHoliday_SkipsHoliday()
    {
        // Arrange
        var schedule = new DefaultEquitiesSchedule(BarInterval.Minute(5));
        var beforeNewYear = new DateTime(2024, 12, 31, 16, 30, 0); // Tuesday after close

        // Act
        var nextTime = schedule.GetNextValidBarTime(beforeNewYear);

        // Assert
        var expected = new DateTime(2025, 1, 2, 9, 30, 0); // Thursday 9:30 AM (skips Jan 1 holiday)
        Assert.Equal(expected, nextTime);
    }

    /// <summary>
    /// Helper method to get a date for a specific day of the week in the given month.
    /// </summary>
    private static DateTime GetDateForDayOfWeek(int year, int month, DayOfWeek targetDayOfWeek)
    {
        var firstOfMonth = new DateTime(year, month, 1);
        var daysToAdd = ((int)targetDayOfWeek - (int)firstOfMonth.DayOfWeek + 7) % 7;
        return firstOfMonth.AddDays(daysToAdd);
    }
}

/// <summary>
/// Tests for <see cref="DefaultEquitiesScheduleFactory"/> to ensure it returns
/// appropriate schedule instances.
/// </summary>
public sealed class DefaultEquitiesScheduleFactoryTests
{
    [Fact]
    public void GetSchedule_ReturnsDefaultEquitiesSchedule()
    {
        // Arrange
        var factory = new DefaultEquitiesScheduleFactory();
        var interval = BarInterval.Minute(5);

        // Act
        var schedule = factory.GetSchedule(interval);

        // Assert
        Assert.IsType<DefaultEquitiesSchedule>(schedule);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(60)]
    [InlineData(1440)]
    public void GetSchedule_DifferentIntervals_ReturnsScheduleWithCorrectInterval(int intervalMinutes)
    {
        // Arrange
        var factory = new DefaultEquitiesScheduleFactory();
        var interval = BarInterval.Minute((ushort)intervalMinutes);

        // Act
        var schedule = factory.GetSchedule(interval);

        // Assert
        var equitiesSchedule = Assert.IsType<DefaultEquitiesSchedule>(schedule);
        Assert.Equal(interval, equitiesSchedule.Interval);
    }

    [Fact]
    public void GetSchedule_MultipleCallsSameInterval_ReturnsDifferentInstances()
    {
        // Arrange
        var factory = new DefaultEquitiesScheduleFactory();
        var interval = BarInterval.Minute(5);

        // Act
        var schedule1 = factory.GetSchedule(interval);
        var schedule2 = factory.GetSchedule(interval);

        // Assert - Different instances but equal values
        Assert.NotSame(schedule1, schedule2);
        Assert.Equal(schedule1, schedule2);
    }
}
