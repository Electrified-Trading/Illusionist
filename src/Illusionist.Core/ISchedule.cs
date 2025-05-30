namespace Illusionist.Core;

public interface ISchedule
{
	BarInterval Interval { get; }

	/// <summary>
	/// Determines whether the given timestamp matches a valid exact interval quantized time.
	/// </summary>
	bool IsValidBarTime(DateTime timestamp);

	/// <summary>
	/// Returns the next valid bar timestamp following the specified point in time.
	/// </summary>
	DateTime GetNextValidBarTime(DateTime prior);
}