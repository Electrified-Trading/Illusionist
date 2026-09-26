namespace Illusionist.Service.Contracts;

/// <summary>
/// The wire layout of a rendered series body. Both layouts are frozen with service v1: a layout
/// change is a new format name, never an edit to an existing one.
/// </summary>
public enum SeriesFormat
{
	/// <summary>
	/// <c>Index,TimestampTicks,Open,High,Low,Close,Volume</c>, invariant culture, LF line ends --
	/// <c>GoldenBarFormatter</c>'s exact layout.
	/// </summary>
	Csv,

	/// <summary>Rows of <c>[timestamp, open, high, low, close, volume]</c> with ISO timestamps.</summary>
	Json,
}
