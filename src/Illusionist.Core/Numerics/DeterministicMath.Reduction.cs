using System.Numerics;

namespace Illusionist.Core.Numerics;

public static partial class DeterministicMath
{
	/// <summary>
	/// Decomposes a finite, non-negative, non-zero <paramref name="x"/> into an exact
	/// <paramref name="mantissa"/> and <paramref name="exponent"/> such that
	/// <c>mantissa * 2^exponent == x</c> exactly (subnormal-aware: a subnormal's mantissa has no
	/// implicit leading bit, and its exponent is the fixed <c>-1074</c>).
	/// </summary>
	private static void DecomposeExact(double x, out BigInteger mantissa, out int exponent)
	{
		var bits = ToBits(x);
		var biasedExponent = (bits >> MantissaBits) & 0x7FF;
		var fraction = bits & MantissaMask;

		if (biasedExponent == 0)
		{
			mantissa = fraction;
			exponent = -1074;
		}
		else
		{
			mantissa = fraction | ImplicitBit;
			exponent = (int)biasedExponent - ExponentBias - MantissaBits;
		}
	}

	/// <summary>
	/// Reduces <c>0 &lt; x &lt;</c> <see cref="MediumRangeLimit"/> modulo pi/2 via a Cody-Waite
	/// reduction against <see cref="PiOver2CodyWaite"/>'s three terms: fast (plain <see cref="double"/>
	/// arithmetic, no <see cref="BigInteger"/>), and accurate with wide margin to spare -- including
	/// at the class of inputs closest to an exact odd multiple of pi/2, where the true remainder is
	/// many orders of magnitude smaller than <paramref name="x"/> itself and a two-term split alone
	/// was measured to leave only ~30 correct bits (see <see cref="PiOver2CodyWaite"/>'s remarks).
	/// </summary>
	private static int ReduceMediumRange(double x, out double y0, out double y1)
	{
		var nd = Math.Round(x / PiOver2CodyWaite.Hi, MidpointRounding.ToEven);
		var r = x - nd * PiOver2CodyWaite.Hi;
		r -= nd * PiOver2CodyWaite.Mid;
		r -= nd * PiOver2CodyWaite.Tail;

		y0 = r;
		y1 = 0.0;
		return (int)nd & 3;
	}

	/// <summary>
	/// Reduces any finite <c>x &gt;=</c> <see cref="MediumRangeLimit"/> modulo pi/2, exactly (up to
	/// <see cref="TwoOverPiScaled"/>'s own ~1280-bit precision): the classic Payne-Hanek technique,
	/// but using <see cref="BigInteger"/> arithmetic derived from <see cref="ArctanReciprocalScaled"/>
	/// in place of <c>fdlibm</c>'s hardcoded 396-word <c>two_over_pi[]</c> table (see the class
	/// remarks in <see cref="DeterministicMath"/>). <paramref name="x"/>'s exact value (mantissa *
	/// 2^exponent) times 2/pi, extracted to the bit position that matters for a correctly-rounded
	/// fractional part, gives both the quadrant <c>n</c> and a remainder accurate to a full
	/// double-double.
	/// </summary>
	private static int ReduceExact(double x, out double y0, out double y1)
	{
		DecomposeExact(x, out var mantissa, out var exponent);

		var shift = ReductionBits - exponent;
		var product = mantissa * TwoOverPiScaled;
		var nRaw = product >> shift;
		var frac = product - (nRaw << shift);

		BigInteger n;
		BigInteger signedFrac;
		if (frac >= BigInteger.One << (shift - 1))
		{
			n = nRaw + 1;
			signedFrac = frac - (BigInteger.One << shift);
		}
		else
		{
			n = nRaw;
			signedFrac = frac;
		}

		var negative = signedFrac.Sign < 0;
		var magnitude = BigInteger.Abs(signedFrac);
		var fraction = ToExtended(magnitude, shift);
		if (negative)
			fraction = new Extended(-fraction.Hi, -fraction.Lo);

		var y = Multiply(fraction, PiOver2);
		y0 = y.Hi;
		y1 = y.Lo;

		return (int)(n & 3);
	}
}
