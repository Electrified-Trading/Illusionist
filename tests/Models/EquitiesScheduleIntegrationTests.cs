namespace Illusionist.Tests.Models;

/// <summary>
/// Integration tests for <see cref="DefaultEquitiesSchedule"/> and <see cref="DefaultEquitiesScheduleFactory"/>
/// working together to demonstrate proper schedule behavior.
/// </summary>
public sealed class EquitiesScheduleIntegrationTests
{
    [Fact]
    public void ScheduleFactory_And_Schedule_WorkTogether()
    {
        // Arrange
        var factory = new DefaultEquitiesScheduleFactory();
        var interval = BarInterval.Minute(15);
        
        // Act
        var schedule = factory.GetSchedule(interval);
        
        // Demonstrate schedule functionality
        var marketTime = new DateTime(2025, 1, 2, 10, 30, 0); // Thursday 10:30 AM
        var isValid = schedule.IsValidBarTime(marketTime);
        var nextTime = schedule.GetNextValidBarTime(marketTime);
        
        // Assert
        Assert.IsType<DefaultEquitiesSchedule>(schedule);
        Assert.True(isValid);
        Assert.Equal(new DateTime(2025, 1, 2, 10, 45, 0), nextTime); // 15 minutes later
    }

    [Fact]
    public void EndToEnd_ScheduleAdvancement_SkipsWeekendsAndHolidays()
    {
        // Arrange
        var factory = new DefaultEquitiesScheduleFactory();
        var schedule = factory.GetSchedule(BarInterval.Hour(1));
        
        var timestamps = new List<DateTime>();
        var current = new DateTime(2025, 1, 3, 15, 0, 0); // Friday 3:00 PM
        
        // Act - Generate next 5 valid bar times
        for (int i = 0; i < 5; i++)
        {
            current = schedule.GetNextValidBarTime(current);
            timestamps.Add(current);
        }
        
        // Assert
        Assert.Equal(new DateTime(2025, 1, 6, 9, 30, 0), timestamps[0]); // Monday 9:30 AM (skips weekend)
        Assert.Equal(new DateTime(2025, 1, 6, 10, 30, 0), timestamps[1]); // Monday 10:30 AM
        Assert.Equal(new DateTime(2025, 1, 6, 11, 30, 0), timestamps[2]); // Monday 11:30 AM
        Assert.Equal(new DateTime(2025, 1, 6, 12, 30, 0), timestamps[3]); // Monday 12:30 PM
        Assert.Equal(new DateTime(2025, 1, 6, 13, 30, 0), timestamps[4]); // Monday 1:30 PM
    }
}
