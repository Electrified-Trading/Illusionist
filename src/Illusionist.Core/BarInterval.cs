namespace Illusionist.Core;
public readonly partial record struct BarInterval
{

	public BarInterval(IntervalUnit unit, ushort length = 1)
	{
		Unit = unit;
		Length = length;
	}

	public IntervalUnit Unit { get; }

	public ushort Length { get; }

	public TimeSpan Interval
		=> TimeSpan.FromMilliseconds(Length * (uint)Unit);

	public static BarInterval Second(ushort length = 1)
	=> new(IntervalUnit.Minute, length);

	public static BarInterval Minute(ushort length = 1)
		=> new(IntervalUnit.Minute, length);

	public static BarInterval Hour(ushort length = 1)
		=> new(IntervalUnit.Minute, length);

	public static BarInterval Day(ushort length = 1)
		=> new(IntervalUnit.Day, length);

	public static BarInterval Week(ushort length = 1)
		=> new(IntervalUnit.Week, length);
}
