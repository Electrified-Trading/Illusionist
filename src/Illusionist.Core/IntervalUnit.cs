namespace Illusionist.Core;

public enum IntervalUnit : uint
{
	Millisecond = 1,
	Second = 1000 * Millisecond,
	Minute = 60 * Second,
	Hour = 60 * Minute,
	Day = 24 * Hour,
	Week = 7 * Day,

	// Month or year are too arbitrary to define as fixed intervals.
}
