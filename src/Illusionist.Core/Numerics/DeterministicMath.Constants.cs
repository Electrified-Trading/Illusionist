using System.Numerics;

namespace Illusionist.Core.Numerics;

public static partial class DeterministicMath
{
	/// <summary>
	/// Bits of precision carried in <see cref="PiScaled"/>, <see cref="TwoOverPiScaled"/> and
	/// <see cref="Ln2Scaled"/> -- enough for <see cref="ReduceExact"/> to correctly reduce any finite
	/// <see cref="double"/> modulo <c>pi/2</c> (the largest normal double's exponent is 1023; this
	/// leaves roughly 250 bits of guard beyond that for the reduction's own fractional-part
	/// extraction, see <see cref="ReduceExact"/>) with generous margin left over for the arctan
	/// series' own per-term truncation (each term below divides with a single <c>floor</c>, so the
	/// accumulated error across a few hundred terms is at most a few hundred units in this scale's
	/// last bit -- utterly below what either use needs).
	/// </summary>
	private const int ReductionBits = 1280;

	/// <summary>
	/// The number of clean high-order bits kept in <see cref="PiOver2CodyWaite"/>'s <c>Hi</c> term --
	/// chosen so that <c>Hi * n</c> is an exact <see cref="double"/> multiplication for any integer
	/// <c>n</c> with up to <c>53 - 33 = 20</c> bits, i.e. any <c>n</c> below <see cref="MediumRangeLimit"/>
	/// (2^16): see <see cref="ReduceMediumRange"/>.
	/// </summary>
	private const int CodyWaiteHiBits = 33;

	/// <summary>
	/// The number of clean high-order bits kept in <see cref="Ln2CodyWaite"/>'s <c>Hi</c> term --
	/// chosen so that <c>Hi * k</c> is an exact <see cref="double"/> multiplication for any integer
	/// <c>k</c> with up to <c>53 - 40 = 13</c> bits, comfortably covering both <see cref="Log"/>'s
	/// binary exponent (at most 1074, for the smallest subnormal) and <see cref="Exp"/>'s reduction
	/// count (at most ~1443, at the underflow boundary) with margin to spare.
	/// </summary>
	private const int Ln2CodyWaiteHiBits = 40;

	/// <summary>
	/// pi, scaled by 2^<see cref="ReductionBits"/>, computed once via Machin's formula
	/// (<c>pi = 16*atan(1/5) - 4*atan(1/239)</c>) in exact <see cref="BigInteger"/> fixed-point
	/// arithmetic -- never a hardcoded digit string and never a call to any platform's own pi
	/// constant. See the class remarks in <see cref="DeterministicMath"/> for why.
	/// </summary>
	private static readonly BigInteger PiScaled = 16 * ArctanReciprocalScaled(5, ReductionBits) - 4 * ArctanReciprocalScaled(239, ReductionBits);

	/// <summary>2/pi at the same scale as <see cref="PiScaled"/>, for <see cref="ReduceExact"/>'s Payne-Hanek-style reduction.</summary>
	private static readonly BigInteger TwoOverPiScaled = (BigInteger.One << (2 * ReductionBits + 1)) / PiScaled;

	/// <summary>pi/2, as a full double-double (re-scaling the same integer <see cref="PiScaled"/> already represents -- dividing by an extra power of two is exact).</summary>
	private static readonly Extended PiOver2 = ToExtended(PiScaled, ReductionBits + 1);

	/// <summary>pi/4, the small-angle fast-path threshold (re-scaling <see cref="PiScaled"/> again, exact for the same reason as <see cref="PiOver2"/>).</summary>
	private static readonly double PiOver4 = ToExtended(PiScaled, ReductionBits + 2).Hi;

	/// <summary>
	/// pi/2 as a Cody-Waite split (<see cref="CodyWaiteHiBits"/>-bit clean <see cref="CodyWaiteSplit.Hi"/>
	/// plus two further full-precision correction terms) for <see cref="ReduceMediumRange"/>'s fast
	/// path -- distinct from <see cref="PiOver2"/>'s "fair" double-double split, which does not have
	/// the deliberately truncated <c>Hi</c> a Cody-Waite reduction depends on for an exact
	/// <c>Hi * n</c> product. Two correction terms, not one: close to an odd multiple of pi/2, a
	/// single <c>Lo</c> term (~53 bits relative to its own, already-tiny magnitude) was measured to
	/// leave only ~30 bits of accuracy in the final reduced angle -- see the change-log entry this
	/// port shipped with. <see cref="CodyWaiteSplit.Mid"/> plus <see cref="CodyWaiteSplit.Tail"/> restores the missing bits.
	/// </summary>
	private static readonly CodyWaiteSplit PiOver2CodyWaite = BuildCodyWaiteSplit(PiScaled, ReductionBits + 1, CodyWaiteHiBits);

	/// <summary>ln(2) = 2*artanh(1/3), scaled by 2^<see cref="ReductionBits"/>, by the same exact-arithmetic method as <see cref="PiScaled"/> (Machin-style, never a memorized constant).</summary>
	private static readonly BigInteger Ln2Scaled = 2 * ArtanhReciprocalScaled(3, ReductionBits);

	/// <summary>
	/// ln(2) as a Cody-Waite split, for <see cref="Log"/>'s and <see cref="Exp"/>'s argument
	/// reductions -- both multiply this by an integer count (<see cref="Log"/>'s binary exponent,
	/// <see cref="Exp"/>'s reduction count) that can reach into the thousands, so (as with
	/// <see cref="PiOver2CodyWaite"/>) a plain two-term double-double is not enough: the same
	/// deliberately-truncated-<c>Hi</c>-plus-two-term-tail shape applies here too.
	/// </summary>
	private static readonly CodyWaiteSplit Ln2CodyWaite = BuildCodyWaiteSplit(Ln2Scaled, ReductionBits, Ln2CodyWaiteHiBits);

	/// <summary>sqrt(2), the mantissa-normalization threshold in <see cref="Log"/>. <see cref="Math.Sqrt(double)"/> is IEEE-754-mandated correctly rounded on every conforming platform (unlike <c>log</c>, <c>exp</c> and <c>cos</c>), so it needs no derivation here.</summary>
	private static readonly double Sqrt2 = Math.Sqrt(2.0);

	/// <summary>2^54, used by <see cref="Log"/> to lift a subnormal input into the normal range before decomposing it (exact: multiplying by a power of two never rounds).</summary>
	private static readonly double TwoPow54 = ScalePow2Normal(1.0, 54);

	/// <summary>
	/// floor(2^<paramref name="bits"/> * atan(1/<paramref name="x"/>)), via the alternating Taylor
	/// series atan(1/x) = sum (-1)^k / ((2k+1) x^(2k+1)). Each term is an exact <see cref="BigInteger"/>
	/// division, truncated toward zero; seed <see cref="ReductionBits"/> carries far more guard than
	/// the handful of truncated units this accumulates across a series that terminates once a term
	/// underflows to zero at this scale.
	/// </summary>
	private static BigInteger ArctanReciprocalScaled(int x, int bits)
	{
		var scale = BigInteger.One << bits;
		var xSquared = (BigInteger)x * x;
		var xPower = (BigInteger)x;
		var sum = BigInteger.Zero;
		var negative = false;
		for (var k = 0; ; k++)
		{
			var term = scale / ((2 * k + 1) * xPower);
			if (term.IsZero)
				break;

			sum = negative ? sum - term : sum + term;
			negative = !negative;
			xPower *= xSquared;
		}

		return sum;
	}

	/// <summary>floor(2^<paramref name="bits"/> * artanh(1/<paramref name="x"/>)) = sum (1/<paramref name="x"/>)^(2k+1) / (2k+1) -- the non-alternating twin of <see cref="ArctanReciprocalScaled"/>, used for ln(2).</summary>
	private static BigInteger ArtanhReciprocalScaled(int x, int bits)
	{
		var scale = BigInteger.One << bits;
		var xSquared = (BigInteger)x * x;
		var xPower = (BigInteger)x;
		var sum = BigInteger.Zero;
		for (var k = 0; ; k++)
		{
			var term = scale / ((2 * k + 1) * xPower);
			if (term.IsZero)
				break;

			sum += term;
			xPower *= xSquared;
		}

		return sum;
	}

	/// <summary>
	/// Converts a non-negative <see cref="BigInteger"/> <paramref name="scaledMagnitude"/>
	/// approximating <c>trueValue * 2^scaleBits</c> into an <see cref="Extended"/> for
	/// <c>trueValue</c>, accurate to a full double-double's ~106 significant bits. Used only to
	/// derive this file's own constants and <see cref="ReduceExact"/>'s per-call residual -- never on
	/// the small-angle hot path.
	/// </summary>
	private static Extended ToExtended(BigInteger scaledMagnitude, int scaleBits)
	{
		var bitLength = (int)scaledMagnitude.GetBitLength();
		var shift = bitLength - 106;
		var top106 = shift > 0 ? scaledMagnitude >> shift : scaledMagnitude << -shift;
		var effectiveScaleBits = scaleBits - shift;

		var hiInt = top106 >> 53;
		var loInt = top106 - (hiInt << 53);

		var hi = ScalePow2Normal((double)hiInt, 53 - effectiveScaleBits);
		var lo = ScalePow2Normal((double)loInt, -effectiveScaleBits);

		TwoSum(hi, lo, out var sumHi, out var sumLo);
		return new Extended(sumHi, sumLo);
	}

	/// <summary>
	/// Builds a Cody-Waite split of <paramref name="scaledMagnitude"/> (interpreted the same way as
	/// <see cref="ToExtended"/>): a <see cref="CodyWaiteSplit.Hi"/> deliberately truncated to
	/// <paramref name="hiBits"/> significant bits (zero below them), so that <c>Hi</c> multiplied by
	/// a modest integer is exact, plus the dropped remainder's own full double-double
	/// (<see cref="CodyWaiteSplit.Mid"/>, <see cref="CodyWaiteSplit.Tail"/>) -- kept as two terms,
	/// not folded into one, so <see cref="ReduceMediumRange"/> retains the ~106 bits <see cref="ToExtended"/>
	/// itself is accurate to, not just the ~53 a single <c>Lo"</c> double could carry.
	/// </summary>
	private static CodyWaiteSplit BuildCodyWaiteSplit(BigInteger scaledMagnitude, int scaleBits, int hiBits)
	{
		var bitLength = (int)scaledMagnitude.GetBitLength();
		var shift = bitLength - hiBits;
		var hiInt = shift > 0 ? scaledMagnitude >> shift : scaledMagnitude << -shift;
		var hiEffectiveScaleBits = scaleBits - shift;

		var hi = ScalePow2Normal((double)hiInt, -hiEffectiveScaleBits);

		var hiAsScaledBack = hiInt << shift;
		var remainder = scaledMagnitude - hiAsScaledBack;
		var remainderExtended = ToExtended(remainder, scaleBits);

		return new CodyWaiteSplit(hi, remainderExtended.Hi, remainderExtended.Lo);
	}

	/// <summary>
	/// pi/2 (or another reduction constant), split for Cody-Waite reduction into a deliberately
	/// truncated <see cref="Hi"/> (exact under multiplication by a modest integer) and a
	/// full-double-double correction (<see cref="Mid"/> + <see cref="Tail"/>). See
	/// <see cref="BuildCodyWaiteSplit"/> and <see cref="ReduceMediumRange"/>.
	/// </summary>
	private readonly record struct CodyWaiteSplit(double Hi, double Mid, double Tail);
}
