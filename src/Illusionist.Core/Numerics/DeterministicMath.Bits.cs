namespace Illusionist.Core.Numerics;

public static partial class DeterministicMath
{
	private const long SignMask = unchecked((long)0x8000_0000_0000_0000);
	private const long ExponentMask = 0x7FF0_0000_0000_0000;
	private const long MantissaMask = 0x000F_FFFF_FFFF_FFFF;
	private const long ImplicitBit = 0x0010_0000_0000_0000;
	private const int ExponentBias = 1023;
	private const int MantissaBits = 52;

	/// <summary>Reinterprets <paramref name="value"/>'s 64 IEEE-754 bits as a <see cref="long"/> -- never a native call, just the same bits.</summary>
	private static long ToBits(double value)
		=> BitConverter.DoubleToInt64Bits(value);

	/// <summary>Reinterprets <paramref name="bits"/> as the <see cref="double"/> whose IEEE-754 representation they are.</summary>
	private static double FromBits(long bits)
		=> BitConverter.Int64BitsToDouble(bits);

	/// <summary>
	/// Exactly multiplies a normal, finite, nonzero <paramref name="value"/> by 2^<paramref name="exponent"/>
	/// by moving only its exponent field -- for constant derivation, where every input and output is
	/// known in advance to stay within the normal range (asserted, not merely assumed).
	/// </summary>
	private static double ScalePow2Normal(double value, int exponent)
	{
		if (value == 0.0)
			return 0.0; // Scaling zero by any power of two is still exactly zero.

		var bits = ToBits(value);
		var exponentBits = (bits >> MantissaBits) & 0x7FF;
		var newExponentBits = exponentBits + exponent;
		if (newExponentBits is <= 0 or >= 0x7FF)
			throw new InvalidOperationException("ScalePow2Normal is for well-scaled constant derivation only; the result left the normal range.");

		var newBits = (bits & unchecked((long)0x800F_FFFF_FFFF_FFFF)) | (newExponentBits << MantissaBits);
		return FromBits(newBits);
	}

	/// <summary>
	/// Exact IEEE-754 <c>scalbn</c>: <paramref name="value"/> * 2^<paramref name="exponent"/> for a
	/// normal, finite, nonzero <paramref name="value"/> and any <paramref name="exponent"/>,
	/// correctly overflowing to infinity or rounding into (or below) the subnormal range -- computed
	/// entirely from the exponent field and, only at the subnormal boundary, a guard/round/sticky
	/// round-to-nearest-even on the mantissa, the same mechanical rule IEEE-754 requires of every
	/// conforming rounding. Used by <c>Exp</c> for its final <c>2^k * exp(r)</c> reconstruction,
	/// where <paramref name="exponent"/> alone (never <paramref name="value"/>) can be large enough
	/// to leave the normal range.
	/// </summary>
	private static double ScalePow2(double value, int exponent)
	{
		var bits = ToBits(value);
		var sign = bits & SignMask;
		var biasedExponent = ((bits >> MantissaBits) & 0x7FF) + exponent;
		var mantissa = bits & MantissaMask;

		if (biasedExponent >= 0x7FF)
			return FromBits(sign | ExponentMask); // Overflow: +-infinity.

		if (biasedExponent >= 1)
			return FromBits(sign | (biasedExponent << MantissaBits) | mantissa); // Still normal.

		// Underflow into subnormal, or all the way to zero: the true value is
		// (1.mantissa) * 2^(biasedExponent - 1023 - 52) with biasedExponent <= 0, so the implicit
		// leading bit must be folded into the fraction itself and the shifted-out bits rounded away.
		var shift = (int)(1 - biasedExponent); // >= 1.
		if (shift > 54)
			return FromBits(sign); // Shifted out entirely, even the round bit: rounds to zero.

		var full = mantissa | ImplicitBit;
		var shifted = full >> shift;
		var roundBit = (full >> (shift - 1)) & 1;
		var stickyMask = (1L << (shift - 1)) - 1;
		var sticky = (full & stickyMask) != 0;

		if (roundBit == 1 && (sticky || (shifted & 1) == 1))
			shifted += 1;

		return FromBits(sign | shifted);
	}
}
