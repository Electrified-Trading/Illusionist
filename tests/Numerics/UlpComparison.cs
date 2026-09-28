namespace Illusionist.Tests.Numerics;

/// <summary>
/// Compares two <see cref="double"/> values by units-in-the-last-place -- the standard technique
/// (Bruce Dawson's "AlmostEqualUlps") of mapping IEEE-754 bit patterns to a monotonically ordered
/// integer so adjacent doubles differ by exactly 1, on both sides of zero and across the positive/
/// negative boundary.
/// </summary>
internal static class UlpComparison
{
	/// <summary>The number of representable <see cref="double"/> values strictly between <paramref name="a"/> and <paramref name="b"/>, plus one.</summary>
	public static long Distance(double a, double b)
	{
		if (double.IsNaN(a) || double.IsNaN(b))
			throw new ArgumentException("ULP distance is undefined for NaN.");

		return Math.Abs(ToOrdered(a) - ToOrdered(b));
	}

	private static long ToOrdered(double value)
	{
		var bits = BitConverter.DoubleToInt64Bits(value);
		return bits < 0 ? unchecked(long.MinValue - bits) : bits;
	}
}
