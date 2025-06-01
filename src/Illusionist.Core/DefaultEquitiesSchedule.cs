namespace Illusionist.Core;

/// <summary>
/// Implements a standard U.S. equity market schedule with 9:30 AM to 4:00 PM trading hours,
/// Monday through Friday, excluding U.S. market holidays for 2024-2025.
/// Assumes bar times are already aligned to the specified interval.
/// </summary>
public readonly partial record struct DefaultEquitiesSchedule(BarInterval Interval) : ISchedule
{
	/// <summary>
	/// Market opening time (9:30 AM).
	/// </summary>
	public static readonly TimeOnly MarketOpen = new(9, 30, 0);

	/// <summary>
	/// Market closing time (4:00 PM).
	/// </summary>
	public static readonly TimeOnly MarketClose = new(16, 0, 0);

	/// <summary>
	/// U.S. market holidays for 2024 and 2025.
	/// </summary>
	private static readonly HashSet<DateOnly> Holidays =
	[
        // 2024 holidays
        new(2024, 1, 1),   // New Year's Day
        new(2024, 1, 15),  // Martin Luther King Jr. Day
        new(2024, 2, 19),  // Presidents' Day
        new(2024, 3, 29),  // Good Friday
        new(2024, 5, 27),  // Memorial Day
        new(2024, 6, 19),  // Juneteenth
        new(2024, 7, 4),   // Independence Day
        new(2024, 9, 2),   // Labor Day
        new(2024, 11, 28), // Thanksgiving Day
        new(2024, 12, 25), // Christmas Day
        
        // 2025 holidays
        new(2025, 1, 1),   // New Year's Day
        new(2025, 1, 20),  // Martin Luther King Jr. Day
        new(2025, 2, 17),  // Presidents' Day
        new(2025, 4, 18),  // Good Friday
        new(2025, 5, 26),  // Memorial Day
        new(2025, 6, 19),  // Juneteenth
        new(2025, 7, 4),   // Independence Day
        new(2025, 9, 1),   // Labor Day
        new(2025, 11, 27), // Thanksgiving Day
        new(2025, 12, 25)  // Christmas Day
    ];

	/// <summary>
	/// Determines whether the given timestamp is a valid bar time during market hours.
	/// </summary>
	/// <param name="timestamp">The timestamp to validate.</param>
	/// <returns>True if the timestamp falls within market hours on a trading day.</returns>
	public bool IsValidBarTime(DateTime timestamp)
	{
		var date = DateOnly.FromDateTime(timestamp);
		var time = TimeOnly.FromDateTime(timestamp);

		// Check if it's a weekend
		if (timestamp.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
			return false;

		// Check if it's a holiday
		if (Holidays.Contains(date))
			return false;

		// Check if it's within market hours
		return time >= MarketOpen && time < MarketClose;
	}

	/// <summary>
	/// Returns the next valid bar timestamp following the specified point in time.
	/// Advances by the configured interval and skips weekends and holidays.
	/// </summary>
	/// <param name="prior">The reference timestamp to advance from.</param>
	/// <returns>The next valid bar timestamp.</returns>
	public DateTime GetNextValidBarTime(DateTime prior)
	{
		var next = prior.Add(Interval.Interval);

		// Keep advancing until we find a valid bar time
		while (!IsValidBarTime(next))
		{
			var date = DateOnly.FromDateTime(next);
			var time = TimeOnly.FromDateTime(next);

			// If we're past market close or it's a weekend/holiday, jump to next trading day at market open
			if (time >= MarketClose
				|| next.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday
				|| Holidays.Contains(date))
			{
				next = GetNextTradingDay(date).ToDateTime(MarketOpen);
			}
			else if (time < MarketOpen)
			{
				// If we're before market open, jump to market open
				next = date.ToDateTime(MarketOpen);
			}
			else
			{
				// We're within the trading day but hit an invalid time, advance by interval
				next = next.Add(Interval.Interval);
			}
		}

		return next;
	}

	/// <summary>
	/// Gets the next valid trading day after the specified date.
	/// </summary>
	/// <param name="date">The reference date.</param>
	/// <returns>The next trading day.</returns>
	private static DateOnly GetNextTradingDay(DateOnly date)
	{
		var nextDate = date.AddDays(1);

		while (IsWeekendOrHoliday(nextDate))
		{
			nextDate = nextDate.AddDays(1);
		}

		return nextDate;
	}

	/// <summary>
	/// Determines if the specified date is a weekend or holiday.
	/// </summary>
	/// <param name="date">The date to check.</param>
	/// <returns>True if the date is a weekend or holiday.</returns>
	private static bool IsWeekendOrHoliday(DateOnly date)
	{
		var dayOfWeek = date.DayOfWeek;
		return dayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday || Holidays.Contains(date);
	}
}
