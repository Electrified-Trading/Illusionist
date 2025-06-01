using System;

class TestAlignment
{
    static void Main()
    {
        // Test the current alignment logic
        var timestamp = new DateTime(2025, 1, 1, 9, 31, 30); // 9:31:30 AM
        var interval = TimeSpan.FromMinutes(1); // 1 minute interval
        
        Console.WriteLine($"Original timestamp: {timestamp:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"Interval: {interval}");
        
        // Current broken logic
        var ticks = timestamp.Ticks;
        var intervalTicks = interval.Ticks;
        var completeIntervals = ticks / intervalTicks;
        var alignedTicks = completeIntervals * intervalTicks;
        var alignedTimestamp = new DateTime(alignedTicks, timestamp.Kind);
        
        Console.WriteLine($"Aligned timestamp: {alignedTimestamp:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"Expected: 2025-01-01 09:31:00");
        
        // What we actually want
        var totalSeconds = (long)timestamp.TimeOfDay.TotalSeconds;
        var intervalSeconds = (long)interval.TotalSeconds;
        var alignedSeconds = (totalSeconds / intervalSeconds) * intervalSeconds;
        var correctAlignment = timestamp.Date.AddSeconds(alignedSeconds);
        
        Console.WriteLine($"Correct alignment: {correctAlignment:yyyy-MM-dd HH:mm:ss}");
    }
}
