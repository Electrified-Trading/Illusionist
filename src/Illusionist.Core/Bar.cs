namespace Illusionist.Core;

/// <summary>
/// Represents a single OHLC (Open, High, Low, Close) bar with volume and timestamp.
/// Immutable structure optimized for high-performance data generation and processing.
/// </summary>
/// <param name="Timestamp">The date and time when this bar period started</param>
/// <param name="Open">The opening price for this bar period</param>
/// <param name="High">The highest price during this bar period</param>
/// <param name="Low">The lowest price during this bar period</param>
/// <param name="Close">The closing price for this bar period</param>
/// <param name="Volume">The total volume traded during this bar period</param>
public readonly record struct Bar(
	DateTime Timestamp,
	decimal Open,
	decimal High,
	decimal Low,
	decimal Close,
	decimal Volume);
